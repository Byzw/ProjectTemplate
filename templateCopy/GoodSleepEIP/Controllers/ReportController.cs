using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodSleepEIP.Controllers
{
  [Authorize]
  [ApiController]
  [Route("api/web")]
  public class ReportController(TaskService _taskService, IConfiguration _config, TokenService _tokenService, PermissionService _permissionService, IDapperHelper _dapper, IParameterService _parameterService, NotificationService _notificationService) : ControllerBase
  {
    private readonly TaskService taskService = _taskService;
    private readonly IConfiguration configuration = _config;
    private readonly IDapperHelper dapper = _dapper;
    private readonly TokenService tokenService = _tokenService;
    private readonly PermissionService permissionService = _permissionService;
    private readonly IParameterService parameterService = _parameterService;
    private readonly NotificationService notificationService = _notificationService;
    private readonly UserClaims userClaims = _tokenService.GetUserClaims();

    /// <summary>
    /// 報表產生任務
    /// </summary>
    [HttpPost("ReportTask")]
    public IActionResult ReportTask(ReportOthersQueryParameters RequestData)
    {
      try
      {
        // 建立報表 Task
        string taskType = "Report";    // 需於 program.cs 中註冊該 taskType
        Guid taskId = taskService.QueueTask(taskType, RequestData);

        // 將前端通知加入
        notificationService.AddDownloadTask(taskId, "02", userClaims.UserId, $"{RequestData.ReportType} 報表", 24);

        return ResponseMsg.Ok(true, "報表任務已加入", new { TaskId = taskId });
      }
      catch (Exception ex)
      {
        return ResponseMsg.Ok(false, $" ReportTask 錯誤，{ex.Message}");
      }
    }
  }
}
