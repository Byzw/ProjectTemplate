using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/task")]
    public class TaskController(TaskService _taskService, IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, IParameterService _parameterService, NotificationService _notificationService) : ControllerBase
    {
        private readonly TaskService taskService = _taskService;
        private readonly IConfiguration configuration = _config;
        private readonly IDapperHelper dapper = _dapper;
        private readonly TokenService tokenService = _tokenService;
        private readonly IParameterService parameterService = _parameterService;
        private readonly NotificationService notificationService = _notificationService;
        private readonly UserClaims userClaims = _tokenService.GetUserClaims();

        /// <summary>
        /// 查詢任務狀態，debug用，目前無用
        /// </summary>
        [HttpGet("status/{taskId}")]
        public async Task<IActionResult> GetTaskStatus(Guid taskId)
        {
            var result = await taskService.GetTaskResult(taskId);
            return result != null ? ResponseMsg.Ok(true, "任務狀態", result) : ResponseMsg.Ok(false, "找不到該任務，可能未排入佇列或已完成");     // 從未成功加入佇列、已經執行完畢 都會找不到
        }

        /// <summary>
        /// 進入點: 觸發 SQL Server 資料庫備份任務
        /// </summary>
        [Permission("SysAdmin", "CanManage")]
        [HttpPost("BackupDatabase")]
        public IActionResult BackupDatabase()
        {
            try
            {
                string taskType = "DatabaseBackup"; // 需於 program.cs 中註冊該 taskType
                Guid taskId = taskService.QueueTask(taskType);

                // 將前端通知加入
                notificationService.AddDownloadTask(taskId, "03", userClaims.UserId, "系統資料庫備份任務", 24);

                return ResponseMsg.Ok(true, "系統資料庫備份任務已加入", new { TaskId = taskId });
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $" BackupDatabase 錯誤，{ex.Message}");
            }
        }
    }
}
