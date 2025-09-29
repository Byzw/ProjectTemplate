using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;
using BCryptHelper = BCrypt.Net.BCrypt;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class UsersManagerController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly DepartmentService departmentService;
        private readonly ILogService logService;
        private readonly UserClaims userClaims;

        public UsersManagerController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, ILogService _logService)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            departmentService = new DepartmentService(dapper);
            logService = _logService;
            userClaims = tokenService.GetUserClaims();
        }

        [Permission("Users", "CanRead")]
        [HttpPost("UsersList")]
        public IActionResult UsersList([FromBody] AgGridRequest request)
        {
            try
            {
                string sql_select = @"SELECT
                        u.UserId, u.Username, u.UserDescription, u.DepartmentId, u.UserEmail, u.LineUserId, u.CreationTime, u.UpdateTime, d.DepartmentName ";

                string sql_from = @$"FROM {DBName.Main}.Users u 
                        LEFT JOIN {DBName.Main}.Departments d ON u.DepartmentId = d.DepartmentId ";

                string default_where = $"u.IsDeleted = '0' ";
                var fieldChangeMap = new List<AgGridFieldChangeMap>
                {
                };

                var gridResult = AgGridHelper.HandleAgGridRequest<dynamic>(dapper, request, sql_select, sql_from, default_where, fieldChangeMap, "UserId asc");
                return Ok(gridResult);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"表格產生錯誤: {ex.Message}" });
            }
        }

        [Permission("Users", "CanRead")]
        [HttpGet("FetchUsersRecord")]
        public IActionResult FetchUsersRecord(int UserId)
        {
            try
            {
                string sqlstr = $"SELECT u.UserId, u.Username, u.UserDescription, u.DepartmentId, u.UserEmail, u.LineUserId, u.CreationTime, u.UpdateTime FROM {DBName.Main}.Users u WHERE u.UserId = @UserId AND u.IsDeleted = 0";
                var Record_list = (List<Users>)dapper.Query<Users>(sqlstr, new { UserId });
                if (Record_list.Count == 0) return ResponseMsg.Ok(false, "找不到使用者資料");

                return ResponseMsg.Ok(true, "User Record.", Record_list[0]);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取使用者資料錯誤: {ex.Message}");
            }
        }

        [Permission("Users", "CanRead")]
        [HttpGet("FetchUserGroupsByUserId")]
        public IActionResult FetchUserGroupsByUserId(int UserId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.UserGroups WHERE UserId = @UserId";
                var recordList = dapper.Query<UserGroups>(sqlstr, new { UserId }).ToList();

                return ResponseMsg.Ok(true, "UserGroups List.", recordList);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取 FetchUserGroupsByUserId 錯誤: {ex.Message}");
            }
        }

        [Permission("Users", "CanRead")]
        [HttpGet("FetchUserRolesByUserId")]
        public IActionResult FetchUserRolesByUserId(int UserId)
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.UserRoles WHERE UserId = @UserId";
                var recordList = dapper.Query<UserRoles>(sqlstr, new { UserId }).ToList();

                return ResponseMsg.Ok(true, "UserRoles List.", recordList);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取 FetchUserRolesByUserId 錯誤: {ex.Message}");
            }
        }

        [Permission("Users", "CanRead")]
        [HttpGet("FetchGroupsList")]
        public IActionResult FetchGroupsList()
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Groups WHERE IsDeleted = 0 ORDER BY PermissionGroupType, GroupName";
                var Record_list = (List<Groups>)dapper.Query<Groups>(sqlstr);

                return ResponseMsg.Ok(true, "Groups List.", Record_list);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取群組列表錯誤: {ex.Message}");
            }
        }

        [Permission("Users", "CanRead")]
        [HttpGet("FetchRolesList")]
        public IActionResult FetchRolesList()
        {
            try
            {
                string sqlstr = $"SELECT * FROM {DBName.Main}.Roles WHERE IsDeleted = 0 ORDER BY RoleName";
                var Record_list = (List<Roles>)dapper.Query<Roles>(sqlstr);
                return ResponseMsg.Ok(true, "Roles List.", Record_list);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"取角色列表錯誤: {ex.Message}");
            }
        }

        // 新增
        [Permission("Users", "CanCreate")]
        [HttpPost("UsersAdd")]
        public IActionResult UsersAdd([FromBody] UsersDTO RequestData)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestData.Password) || RequestData.Password.Length < 6 || RequestData.Password.Length > 20) return ResponseMsg.Ok(false, "密碼長度需為 6-20 個字元");
                else RequestData.Password = BCryptHelper.HashPassword(RequestData.Password.Trim(), BCryptHelper.GenerateSalt());

                // 檢查 Username 是否已存在
                var userCount = Convert.ToInt32(dapper.QueryScalar($@"SELECT COUNT(*) FROM {DBName.Main}.Users WHERE Username = @Username AND IsDeleted = 0", new { RequestData.Username }));
                if (userCount > 0) return ResponseMsg.Ok(false, "使用者名稱已存在");

                // 新增紀錄
                int insertUserId = Convert.ToInt32(dapper.QueryScalar(@$"
                    INSERT INTO {DBName.Main}.Users (Username, Password, UserDescription, DepartmentId, UserEmail, LineUserId, CreationTime, UpdateTime)
                    VALUES (@Username, @Password, @UserDescription, @DepartmentId, @UserEmail, @LineUserId, GETDATE(), GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS int);", RequestData));
                if (!(insertUserId > 0)) return ResponseMsg.Ok(false, "UsersAdd 檢查錯誤，請告知系統管理員");

                // 新增 UserGroups
                var groups = RequestData.Groups.Select((groupId, index) => new UserGroups
                {
                    UserId = insertUserId,
                    GroupId = groupId
                }).ToList();
                int ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.UserGroups (UserId, GroupId) VALUES (@UserId, @GroupId)", groups);
                if (ex_count != RequestData.Groups.Count) return ResponseMsg.Ok(false, "UsersAdd UserGroups 新增檢查錯誤，請告知系統管理員");

                // 新增 UserRoles
                var roles = RequestData.Roles.Select((roleId, index) => new UserRoles
                {
                    UserId = insertUserId,
                    RoleId = roleId
                }).ToList();
                ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", roles);
                if (ex_count != RequestData.Roles.Count) return ResponseMsg.Ok(false, "UsersAdd UserRoles 新增檢查錯誤，請告知系統管理員");

                // 記錄日誌
                logService.CreateLog(logType: "info", actionType: "create", moduleName: "Users", username: userClaims.Username, logData: new { RequestData });

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"UsersAdd 錯誤: {ex.Message}"); }
        }

        // 修改
        [Permission("Users", "CanUpdate")]
        [HttpPost("UsersEdit")]
        public IActionResult PermissionsEdit([FromBody] UsersDTO RequestData)
        {
            try
            {
                if (!(RequestData.UserId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");

                if (!string.IsNullOrEmpty(RequestData.Password))
                {
                    if (RequestData.Password.Length < 6 || RequestData.Password.Length > 20) return ResponseMsg.Ok(false, "密碼長度需為 6-20 個字元");
                    RequestData.Password = BCryptHelper.HashPassword(RequestData.Password.Trim(), BCryptHelper.GenerateSalt());
                }

                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Users SET
                                                {(string.IsNullOrEmpty(RequestData.Password) ? "" : "Password = @Password,")}
                                                UserDescription = @UserDescription, DepartmentId = @DepartmentId, UserEmail = @UserEmail, LineUserId = @LineUserId, UpdateTime = GETDATE()
                                                WHERE UserId = @UserId", RequestData);
                if (ex_count != 1) return ResponseMsg.Ok(false, "UsersEdit 檢查錯誤，請告知系統管理員");

                // 更新 UserGroups
                // 刪除 UserGroups 舊資料
                dapper.Execute(@$"DELETE FROM {DBName.Main}.UserGroups WHERE UserId = @UserId", new { RequestData.UserId });
                // 插入新的 `UserGroups`（如果有資料）
                var groups = RequestData.Groups.Select((groupId, index) => new UserGroups
                {
                    UserId = RequestData.UserId,
                    GroupId = groupId
                }).ToList();
                ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.UserGroups (UserId, GroupId) VALUES (@UserId, @GroupId)", groups);
                if (ex_count != RequestData.Groups.Count) return ResponseMsg.Ok(false, "UsersEdit UserGroups 新增檢查錯誤，請告知系統管理員");

                // 更新 UserRoles
                // 刪除 UserRoles 舊資料
                dapper.Execute(@$"DELETE FROM {DBName.Main}.UserRoles WHERE UserId = @UserId", new { RequestData.UserId });
                // 插入新的 `UserRoles`（如果有資料）
                var roles = RequestData.Roles.Select((roleId, index) => new UserRoles
                {
                    UserId = RequestData.UserId,
                    RoleId = roleId
                }).ToList();
                ex_count = dapper.Execute(@$"INSERT INTO {DBName.Main}.UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)", roles);
                if (ex_count != RequestData.Roles.Count) return ResponseMsg.Ok(false, "UsersEdit UserRoles 新增檢查錯誤，請告知系統管理員");

                // 記錄日誌
                logService.CreateLog(logType: "info", actionType: "update", moduleName: "Users", username: userClaims.Username, logData: new { RequestData });

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, "UsersEdit 錯誤:" + ex.Message); }
        }

        // 刪除
        [Permission("Users", "CanDelete")]
        [HttpGet("UsersDel")]
        public IActionResult UsersDel(int UserId)
        {
            try
            {
                if (!(UserId > 0)) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Users SET IsDeleted = '1' WHERE UserId = @UserId", new { UserId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "UsersDel 檢查錯誤，請告知系統管理員");

                // 記錄日誌
                logService.CreateLog(logType: "info", actionType: "delete", moduleName: "Users", username: userClaims.Username, logData: new { UserId });

                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "UsersDel 發生內部錯誤"); }
        }

        // 使用者修改自己密碼 (所有人)
        [HttpPost("UserChangePassword")]
        public IActionResult UserChangePassword([FromBody] UserChangePasswordDTO RequestData)
        {
            try
            {
                if (string.IsNullOrEmpty(RequestData.NewPassword) || RequestData.NewPassword.Length < 6 || RequestData.NewPassword.Length > 20) return ResponseMsg.Ok(false, "密碼長度需為 6-20 個字元");
                if (RequestData.NewPassword != RequestData.ConfirmPassword) return ResponseMsg.Ok(false, "新密碼與確認密碼不一致");

                RequestData.NewPassword = BCryptHelper.HashPassword(RequestData.NewPassword.Trim(), BCryptHelper.GenerateSalt());

                // 更新紀錄
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Users SET Password = @NewPassword, UpdateTime = GETDATE() WHERE UserId = @UserId", new { RequestData.NewPassword, userClaims.UserId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "UserChangePassword 檢查錯誤，請告知系統管理員");

                // 記錄日誌
                logService.CreateLog(logType: "info", actionType: "update", moduleName: "Users", username: userClaims.Username, logData: new { userId = userClaims.UserId, action = "changePassword" });

                return ResponseMsg.Ok(true, "");
            }
            catch (Exception ex) { return ResponseMsg.Ok(false, $"UsersAdd 錯誤: {ex.Message}"); }
        }
    }
}