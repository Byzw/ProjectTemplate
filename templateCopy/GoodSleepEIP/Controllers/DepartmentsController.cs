using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly DepartmentService departmentService;

        private readonly UserClaims userClaims;

        public DepartmentsController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            departmentService = new DepartmentService(dapper);
            userClaims = tokenService.GetUserClaims();
        }

        [Permission("Departments", "CanRead")]
        [HttpPost("DepartmentsList")]
        public IActionResult DepartmentsList([FromBody] AgGridRequest request)
        {
            try
            {
                string sql_select = @"SELECT
                        d.DepartmentId, 
                        d.DepartmentName, 
                        d.DepartmentDescription,
                        d.ParentDepartmentId,
                        parent_d.DepartmentName AS ParentDepartmentName,
                        DepartmentLevel.Description AS DepartmentLevel,
                        d.IsDeleted,
                        d.CreationTime,
                        d.UpdateTime ";

                string sql_from = @$"FROM {DBName.Main}.Departments d 
                        LEFT JOIN {DBName.Main}.Departments parent_d ON d.ParentDepartmentId = parent_d.DepartmentId
                        LEFT JOIN {DBName.Main}.Parameter AS DepartmentLevel ON d.DepartmentLevel = DepartmentLevel.Code AND DepartmentLevel.Category = 'DepartmentLevel' ";

                string default_where = $"d.IsDeleted = '0' ";
                var fieldChangeMap = new List<AgGridFieldChangeMap>
                {
                    new() { OriginalFieldName = "DepartmentLevel", NewFieldName = "DepartmentLevel.Description" },
                    new() { OriginalFieldName = "DepartmentName", NewFieldName = "d.DepartmentName" },
                    new() { OriginalFieldName = "DepartmentDescription", NewFieldName = "d.DepartmentDescription" },
                    new() { OriginalFieldName = "ParentDepartmentName", NewFieldName = "parent_d.DepartmentName" }
                };

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, fieldChangeMap, "GroupId asc");
                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("Departments", "CanRead")]
        [HttpGet("FetchDepartmentsList")]
        public IActionResult FetchDepartmentsList()
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Departments WHERE IsDeleted = 0 ORDER BY DepartmentLevel, DepartmentName";
                var Record_list = (List<Departments>)dapper.Query<Departments>(sqlstr);

                return ResponseMsg.Ok(true, "Department List.", Record_list);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取部門列表錯誤: {ex.Message}");
            }
        }

        [Permission("Departments", "CanRead")]
        [HttpGet("FetchUsersList")]
        public IActionResult FetchUsersList()
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Users WHERE IsDeleted = 0 ORDER BY DepartmentId, UserId";
                var Record_list = (List<Users>)dapper.Query<Users>(sqlstr);

                return ResponseMsg.Ok(true, "Users List.", Record_list);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取使用者列表錯誤: {ex.Message}");
            }
        }

        [Permission("Departments", "CanRead")]
        [HttpGet("FetchDepartmentRecord")]
        public IActionResult FetchDepartmentRecord(int DepartmentId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Departments WHERE DepartmentId = @DepartmentId AND IsDeleted = 0";
                var Record_list = (List<Departments>)dapper.Query<Departments>(sqlstr, new { DepartmentId });
                if (Record_list.Count == 0) return ResponseMsg.Ok(false, "找不到部門資料");

                return ResponseMsg.Ok(true, "Department Record.", Record_list[0]);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取角色資料錯誤: {ex.Message}");
            }
        }

        [Permission("Departments", "CanRead")]
        [HttpGet("FetchDepartmentManagersRecord")]
        public IActionResult FetchDepartmentManagersRecord(int DepartmentId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.DepartmentManagers WHERE DepartmentId = @DepartmentId ORDER BY ManagerOrder ASC";
                var recordList = dapper.Query<DepartmentManagers>(sqlstr, new { DepartmentId }).ToList();

                return ResponseMsg.Ok(true, "DepartmentManagers List.", recordList);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取 FetchDepartmentManagersRecord 錯誤: {ex.Message}");
            }
        }

        // 新增
        [Permission("Departments", "CanCreate")]
        [HttpPost("DepartmentsAdd")]
        public IActionResult DepartmentsAdd([FromBody] DepartmentsDTO RequestData)
        {
            try
            {
                // 新增紀錄
                int insertDepartmentId = (int)dapper.QueryScalar(@$"INSERT INTO {DBName.Main}.Departments (DepartmentName, DepartmentDescription, ParentDepartmentId, DepartmentLevel, IsDeleted, CreationTime, UpdateTime)
                                                VALUES (@DepartmentName, @DepartmentDescription, @ParentDepartmentId, @DepartmentLevel, 0, GETDATE(), GETDATE());
                                                SELECT CAST(SCOPE_IDENTITY() AS int);", RequestData);
                if (!(insertDepartmentId > 0)) return ResponseMsg.Ok(false, "DepartmentsAdd 檢查錯誤，請告知系統管理員");

                // 插入新的 `DepartmentManagers`（如果有資料）
                if (RequestData.Managers.Count > 0)
                {
                    // int[] Managers 轉換至 DepartmentManagers
                    var managers = RequestData.Managers.Select((userId, index) => new DepartmentManagers
                    {
                        DepartmentId = insertDepartmentId,
                        UserId = userId,
                        ManagerOrder = index + 1 // 重新依序分配 order，從 1 開始
                    }).ToList();

                    int ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.DepartmentManagers (DepartmentId, UserId, ManagerOrder) VALUES (@DepartmentId, @UserId, @ManagerOrder)", managers);
                    if (ex_count != RequestData.Managers.Count) return ResponseMsg.Ok(false, "GroupsEdit DepartmentManagers 新增檢查錯誤，請告知系統管理員");
                }

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"DepartmentsAdd 錯誤: {ex.Message}"); }
        }

        // 修改
        [Permission("Departments", "CanUpdate")]
        [HttpPost("DepartmentsEdit")]
        public IActionResult DepartmentsEdit([FromBody] DepartmentsDTO RequestData)
        {
            try
            {
                if (!(RequestData.DepartmentId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                // 更新 `Departments` 資料
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Departments SET DepartmentName = @DepartmentName, DepartmentDescription = @DepartmentDescription, ParentDepartmentId = @ParentDepartmentId, DepartmentLevel = @DepartmentLevel WHERE DepartmentId = @DepartmentId", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "DepartmentsEdit 檢查錯誤，請告知系統管理員");

                // 刪除 `DepartmentManagers` 資料
                ex_count = dapper.Execute(@$"DELETE FROM {DBName.Main}.DepartmentManagers WHERE DepartmentId = @DepartmentId", RequestData);

                // 插入新的 `DepartmentManagers`（如果有資料）
                if (RequestData.Managers.Count > 0)
                {
                    // int[] Managers 轉換至 DepartmentManagers
                    var managers = RequestData.Managers.Select((userId, index) => new DepartmentManagers
                    {
                        DepartmentId = RequestData.DepartmentId,
                        UserId = userId,
                        ManagerOrder = index + 1 // 重新依序分配 order，從 1 開始
                    }).ToList();

                    ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.DepartmentManagers (DepartmentId, UserId, ManagerOrder) VALUES (@DepartmentId, @UserId, @ManagerOrder)", managers);
                    if (ex_count != RequestData.Managers.Count) return ResponseMsg.Ok(false, "GroupsEdit DepartmentManagers 新增檢查錯誤，請告知系統管理員");
                }

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, "DepartmentsEdit 錯誤:" + ex.Message); }
        }

        // 刪除
        [Permission("Departments", "CanDelete")]
        [HttpGet("DepartmentsDel")]
        public IActionResult DepartmentsDel(int DepartmentId)
        {
            try
            {
                if (!(DepartmentId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Departments SET IsDeleted = '1' WHERE DepartmentId = @DepartmentId", new { DepartmentId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "DepartmentsDel 檢查錯誤，請告知系統管理員");
                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "DepartmentsDel 發生內部錯誤"); }
        }
    }
}