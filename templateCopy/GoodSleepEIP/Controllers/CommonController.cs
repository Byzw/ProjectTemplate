using Dapper;
using GoodSleepEIP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/web")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDapperHelper dapper;
        private readonly TokenService tokenService;
        private readonly IParameterService parameterService;

        private readonly UserClaims userClaims;

        public CommonController(IConfiguration _config, TokenService _tokenService, IDapperHelper _dapper, IParameterService _parameterService)
        {
            configuration = _config;
            dapper = _dapper;
            tokenService = _tokenService;
            parameterService = _parameterService;
            userClaims = tokenService.GetUserClaims();
        }
        
        [Authorize]
        [HttpGet("GetAllowedExtensionsString")]
        public IActionResult GetAllowedExtensionsString()
        {
            try
            {
                return ResponseMsg.Ok(true, "AllowedExtensions", parameterService.GetAllowedExtensionsString());
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"發生內部錯誤: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("GetAllNotifications")]
        public IActionResult GetAllNotifications()
        {
            try
            {
                string sql = @$"SELECT 
                                    n.NotificationId,
                                    n.UserId,
                                    n.NotificationMessageContent,
                                    n.NotificationErrorMessageContent,
                                    n.NotificationPriority,
                                    NotificationType.Description AS NotificationType,
                                    n.NotificationLink,
                                    n.IsLinkNewWindow,
                                    n.IsInternalLink,
                                    n.IsBlob,
                                    n.ProductionPercentage,
                                    n.ReadTime,
                                    n.CreationTime,
                                    n.NotificationExpiresTime
                                FROM {DBName.Main}.Notifications n
                                LEFT JOIN {DBName.Main}.Parameter AS NotificationType ON n.NotificationType = NotificationType.Code AND NotificationType.Category = 'NotificationType'
                                WHERE IsDeleted = 0 AND (UserId = @UserId OR UserId IS NULL)
                                    AND (NotificationExpiresTime IS NULL OR NotificationExpiresTime > GETDATE())  -- 過期判斷
                                    AND (
                                        ReadTime IS NULL    -- 未讀訊息
                                        OR DATEDIFF(HOUR, ReadTime, GETDATE()) <= 24    -- 或已讀時間在24小時以內
                                    )
                                ORDER BY NotificationPriority DESC, CreationTime DESC";
                var notifications = dapper.Query<Notifications>(sql, new { userClaims.UserId }).ToList();
                return ResponseMsg.Ok(true, "Notifications", notifications);  // 返回所有訊息
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"GetAllNotifications 錯誤: {ex.Message}");
            }
        }

        [HttpGet("MarkNotificationRead")]
        public IActionResult MarkNotificationRead(Guid NotificationId)
        {
            try
            {
                if (NotificationId == Guid.Empty) return ResponseMsg.Ok(false, "ID 不可為空值");
                int ex_count = dapper.Execute(@$"UPDATE {DBName.Main}.Notifications SET ReadTime = GETDATE() WHERE NotificationId = @NotificationId AND UserId = @UserId", new { NotificationId, userClaims.UserId });
                if (ex_count != 1) return ResponseMsg.Ok(false, "MarkNotificationRead 檢查錯誤，請告知系統管理員");
                return ResponseMsg.Ok(true, "");
            }
            catch { return ResponseMsg.Ok(false, "MarkNotificationRead 發生內部錯誤"); }
        }

        [HttpGet("GetCompanyList")]
        public IActionResult GetCompanyList()
        {
            try
            {
                string sql = @$"SELECT * FROM {DBName.Main}.Company WHERE IsActive = 1 ORDER BY CompanyName";
                var companies = dapper.Query<Company>(sql).ToList();
                return ResponseMsg.Ok(true, "Company List", companies);
            }
            catch (Exception ex)
            {
                return ResponseMsg.Ok(false, $"GetCompanyList 錯誤: {ex.Message}");
            }
        }

    }
}