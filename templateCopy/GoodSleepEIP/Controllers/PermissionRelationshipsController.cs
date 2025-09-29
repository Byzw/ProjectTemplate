using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class PermissionRelationshipsController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;

        private readonly UserClaims userClaims;

        public PermissionRelationshipsController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            userClaims = tokenService.GetUserClaims();
        }

        [Permission("PermissionRelationships", "CanRead")]
        [HttpGet("FetchPermissionsList")]
        public IActionResult FetchPermissionsList()
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Permissions WHERE IsDeleted = 0 ORDER BY ModuleName, PermissionModuleSubType";
                var Record_list = (List<Permissions>)dapper.Query<Permissions>(sqlstr);

                return ResponseMsg.Ok(true, "Permission List.", Record_list);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取權限資料錯誤: {ex.Message}");
            }
        }

        // Roles 角色區 -----------------------------------------------------------------------------------------------------
        [Permission("PermissionRelationships", "CanRead")]
        [HttpPost("RolesList")]
        public IActionResult RolesList([FromBody] AgGridRequest request)
        {
            try
            {
                string sql_select = @"SELECT r.* ";
                string sql_from = @$"FROM {DBName.Main}.Roles r ";
                string default_where = $"r.IsDeleted = '0' ";

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, new List<AgGridFieldChangeMap> { }, "RoleId asc");

                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("PermissionRelationships", "CanRead")]
        [HttpGet("FetchRolesRecord")]
        public IActionResult FetchRolesRecord(int RoleId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Roles WHERE RoleId = @RoleId ";
                var Record_list = (List<object>)dapper.Query<object>(sqlstr, new { RoleId });

                return ResponseMsg.Ok(true, "Role Records.", new Dictionary<string, object> { { "Record", Record_list } });
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取角色資料錯誤: {ex.Message}");
            }
        }

        [Permission("PermissionRelationships", "CanRead")]
        [HttpGet("FetchRolePermissions")]
        public IActionResult FetchRolePermissions(int RoleId)
        {
            try
            {
                string sqlstr = $"SELECT PermissionId FROM {DBName.Main}.RolePermissions WHERE RoleId = @RoleId";
                var recordList = dapper.Query<int>(sqlstr, new { RoleId }).ToList();

                return ResponseMsg.Ok(true, "RolePermissions PermissionId List.", recordList);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取 RolePermissions 錯誤: {ex.Message}");
            }
        }

        // 新增
        [Permission("PermissionRelationships", "CanCreate")]
        [HttpPost("RolesAdd")]
        public IActionResult RolesAdd([FromBody] Roles RequestData)
        {
            try
            {
                // 新增紀錄
                int ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.Roles (RoleName, RoleDescription, IsAdmin, IsSystemReserved, IsDeleted, CreationTime)
                                                VALUES (@RoleName, @RoleDescription, @IsAdmin, 0, 0, GETDATE())", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "RolesAdd 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"RolesAdd 錯誤: {ex.Message}"); }
        }

        // 修改
        [Permission("PermissionRelationships", "CanUpdate")]
        [HttpPost("RolesEdit")]
        public IActionResult RolesEdit([FromBody] RolesWithPermissionsList RequestData)
        {
            try
            {
                if (!(RequestData.RoleId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                
                 // 更新 `Roles` 資料
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Roles SET
                                                RoleName = @RoleName, RoleDescription = @RoleDescription, IsAdmin = @IsAdmin
                                                WHERE RoleId = @RoleId", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "RolesEdit 檢查錯誤，請告知系統管理員");

                // 刪除 `RolePermissions` 資料
                ex_count = dapper.Execute(@$"DELETE FROM {DBName.Main}.RolePermissions WHERE RoleId = @RoleId", RequestData);
                
                // 插入新的 `RolePermissions`（如果有資料）
                if (RequestData.RolePermissions.Count > 0)
                {
                    var rolePermissions = RequestData.RolePermissions.Select(x => new RolePermissions { RoleId = RequestData.RoleId, PermissionId = x });
                    ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.RolePermissions (RoleId, PermissionId) VALUES (@RoleId, @PermissionId)", rolePermissions);
                    if (ex_count != RequestData.RolePermissions.Count) return ResponseMsg.Ok(false, "RolesEdit RolePermissions 新增檢查錯誤，請告知系統管理員");
                }
                
                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, "RolesEdit 錯誤:" + ex.Message); }
        }

        // 刪除
        [Permission("PermissionRelationships", "CanDelete")]
        [HttpGet("RolesDel")]
        public IActionResult RolesDel(int RoleId)
        {
            try
            {
                if (!(RoleId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Roles SET IsDeleted = '1' WHERE RoleId = @RoleId AND IsSystemReserved = '0'", new { RoleId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "RolesDel 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "RolesDel 發生內部錯誤"); }
        }

        // Groups 群組區 -----------------------------------------------------------------------------------------------------
        [Permission("PermissionRelationships", "CanRead")]
        [HttpPost("GroupsList")]
        public IActionResult GroupsList([FromBody] AgGridRequest request)
        {
            try
            {
                string sql_select = @"SELECT
                                            g.GroupId,
                                            g.GroupName,
                                            g.GroupDescription,
                                            PermissionGroupType.Description AS PermissionGroupType,
                                            g.IsAdmin,
                                            g.IsSystemReserved,
                                            g.IsDeleted,
                                            g.CreationTime ";
                string sql_from = @$"FROM {DBName.Main}.Groups g 
                                        LEFT JOIN {DBName.Main}.Parameter AS PermissionGroupType ON g.PermissionGroupType = PermissionGroupType.Code AND PermissionGroupType.Category = 'PermissionGroupType' ";
                string default_where = $"g.IsDeleted = '0' ";
                var fieldChangeMap = new List<AgGridFieldChangeMap>
                {
                    new() { OriginalFieldName = "PermissionGroupType", NewFieldName = "PermissionGroupType.Description" },
                    new() {
                        OriginalFieldName = "IsAdmin",
                        AgGridValueChangeMap =
                        {
                            new() { OriginalValue = "是", NewValue = "1" }, new() { OriginalValue = "否", NewValue = "0" }
                        }
                    }
                };

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, fieldChangeMap, "GroupId asc");
                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("PermissionRelationships", "CanRead")]
        [HttpGet("FetchGroupsRecord")]
        public IActionResult FetchGroupsRecord(int GroupId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Groups WHERE GroupId = @GroupId ";
                var Record_list = (List<Groups>)dapper.Query<Groups>(sqlstr, new { GroupId });
                if (Record_list.Count == 0) return ResponseMsg.Ok(false, "找不到群組資料");

                return ResponseMsg.Ok(true, "Group Record.", Record_list[0]);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取角色資料錯誤: {ex.Message}");
            }
        }

        [Permission("PermissionRelationships", "CanRead")]
        [HttpGet("FetchGroupPermissions")]
        public IActionResult FetchGroupPermissions(int GroupId)
        {
            try
            {
                string sqlstr = $"SELECT PermissionId FROM {DBName.Main}.GroupPermissions WHERE GroupId = @GroupId";
                var recordList = dapper.Query<int>(sqlstr, new { GroupId }).ToList();

                return ResponseMsg.Ok(true, "GroupPermissions PermissionId List.", recordList);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取 GroupPermissions 錯誤: {ex.Message}");
            }
        }

        // 新增
        [Permission("PermissionRelationships", "CanCreate")]
        [HttpPost("GroupsAdd")]
        public IActionResult GroupsAdd([FromBody] Groups RequestData)
        {
            try
            {
                // 新增紀錄
                int ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.Groups (GroupName, GroupDescription, PermissionGroupType, IsAdmin, IsSystemReserved, IsDeleted, CreationTime)
                                        VALUES (@GroupName, @GroupDescription, @PermissionGroupType, @IsAdmin, 0, 0, GETDATE())", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "GroupsAdd 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"GroupsAdd 錯誤: {ex.Message}"); }
        }

        // 修改
        [Permission("PermissionRelationships", "CanUpdate")]
        [HttpPost("GroupsEdit")]
        public IActionResult GroupsEdit([FromBody] GroupsWithPermissionsList RequestData)
        {
            try
            {
                if (!(RequestData.GroupId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                // 更新 `Groups` 資料
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Groups SET
                                        GroupName = @GroupName, GroupDescription = @GroupDescription, PermissionGroupType = @PermissionGroupType, IsAdmin = @IsAdmin
                                        WHERE GroupId = @GroupId", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "GroupsEdit 檢查錯誤，請告知系統管理員");

                // 刪除 `GroupPermissions` 資料
                ex_count = dapper.Execute(@$"DELETE FROM {DBName.Main}.GroupPermissions WHERE GroupId = @GroupId", RequestData);

                // 插入新的 `GroupPermissions`（如果有資料）
                if (RequestData.GroupPermissions.Count > 0)
                {
                    var groupPermissions = RequestData.GroupPermissions.Select(x => new GroupPermissions { GroupId = RequestData.GroupId, PermissionId = x });
                    ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.GroupPermissions (GroupId, PermissionId) VALUES (@GroupId, @PermissionId)", groupPermissions);
                    if (ex_count != RequestData.GroupPermissions.Count) return ResponseMsg.Ok(false, "GroupsEdit GroupPermissions 新增檢查錯誤，請告知系統管理員");
                }

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, "GroupsEdit 錯誤:" + ex.Message); }
        }

        // 刪除
        [Permission("PermissionRelationships", "CanDelete")]
        [HttpGet("GroupsDel")]
        public IActionResult GroupsDel(int GroupId)
        {
            try
            {
                if (!(GroupId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Groups SET IsDeleted = '1' WHERE GroupId = @GroupId AND IsSystemReserved = '0'", new { GroupId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "GroupsDel 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "GroupsDel 發生內部錯誤"); }
        }
    }
}