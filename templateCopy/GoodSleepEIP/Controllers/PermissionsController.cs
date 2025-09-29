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
    public class PermissionsController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;

        private readonly UserClaims userClaims;

        public PermissionsController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            userClaims = tokenService.GetUserClaims();
        }

        [Permission("Permissions", "CanRead")]
        [HttpPost("PermissionsList")]
        public IActionResult PermissionsList([FromBody] AgGridRequest request)
        {
            try
            {
                string sql_select = @"SELECT p.* ";
                string sql_from = @$"FROM {DBName.Main}.Permissions p ";
                string default_where = $"p.IsDeleted = '0' ";

                //string ChangeMapJson = @"";
                //List<AgGridFieldChangeMap> fieldChangeMap = JsonSerializer.Deserialize<List<AgGridFieldChangeMap>>(ChangeMapJson) ?? new List<AgGridFieldChangeMap> { };

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, new List<AgGridFieldChangeMap> { }, "PermissionId asc");

                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("Permissions", "CanRead")]
        [HttpGet("FetchPermissionsRecord")]
        public IActionResult FetchPermissionsRecord(int PermissionId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Permissions WHERE PermissionId = @PermissionId ";
                var Record_list = (List<Permissions>)dapper.Query<Permissions>(sqlstr, new { PermissionId });
                if (Record_list.Count == 0) return ResponseMsg.Ok(false, "找不到權限資料");

                return ResponseMsg.Ok(true, "Permission Record.", Record_list[0]);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取權限資料錯誤: {ex.Message}");
            }
        }

        // 新增
        [Permission("Permissions", "CanCreate")]
        [HttpPost("PermissionsAdd")]
        public IActionResult PermissionsAdd([FromBody] Permissions RequestData)
        {
            try
            {
                // 新增紀錄
                int ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.Permissions (ModuleName, PermissionModuleSubType, PermissionDescription, IsPublic, CanRead, CanReadAll, CanCreate, CanUpdate, CanDelete, CanManage, IsSystemReserved, IsDeleted, CreationTime)
                                                VALUES (@ModuleName, @PermissionModuleSubType, @PermissionDescription, @IsPublic, @CanRead, @CanReadAll, @CanCreate, @CanUpdate, @CanDelete, @CanManage, 0, 0, GETDATE())", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "PermissionsAdd 檢查錯誤，請告知系統管理員");
                
                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"PermissionsAdd 錯誤: {ex.Message}"); }
        }

        // 修改
        [Permission("Permissions", "CanUpdate")]
        [HttpPost("PermissionsEdit")]
        public IActionResult PermissionsEdit([FromBody] Permissions RequestData)
        {
            try
            {
                if (!(RequestData.PermissionId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Permissions SET
                                                ModuleName = @ModuleName, PermissionModuleSubType = @PermissionModuleSubType, PermissionDescription = @PermissionDescription, IsPublic = @IsPublic, CanRead = @CanRead, CanReadAll = @CanReadAll, CanCreate = @CanCreate, CanUpdate = @CanUpdate, CanDelete = @CanDelete, CanManage = @CanManage
                                                WHERE PermissionId = @PermissionId", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "PermissionsEdit 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, "PermissionsEdit 錯誤:" + ex.Message); }
        }

        // 刪除
        [Permission("Permissions", "CanDelete")]
        [HttpGet("PermissionsDel")]
        public IActionResult PermissionsDel(int PermissionId)
        {
            try
            {
                if (!(PermissionId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Permissions SET IsDeleted = '1' WHERE PermissionId = @PermissionId AND IsSystemReserved = '0'", new { PermissionId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "PermissionsDel 檢查錯誤，請告知系統管理員");

                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "PermissionsDel 發生內部錯誤"); }
        }

    }
}