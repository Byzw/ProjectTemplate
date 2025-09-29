using Dapper;
using GoodSleepEIP.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace GoodSleepEIP
{
    public class NotificationService(IDapperHelper _dapper)
    {
        private readonly IDapperHelper dapper = _dapper;

        /// <summary>
        /// 產生文件任務訊息
        /// </summary>
        public void AddDownloadTask(Guid NotificationId, string NotificationType, int UserId, string NotificationMessageContent, int ExpiresHours)
        {
            try
            {
                var notification = new Notifications
                {
                    NotificationId = NotificationId,
                    UserId = UserId,
                    NotificationMessageContent = NotificationMessageContent,
                    NotificationPriority = 4,   // 訊息的優先級，越大越重要，如 1一般，2警告，3錯誤，4立即資訊(如文件下載)
                    NotificationType = NotificationType,    // 01 系統通知, 02 文件產生, 03 系統備份
                    NotificationLink = $"/api/attachment/download/{NotificationId}",
                    IsLinkNewWindow = false,
                    IsInternalLink = true,
                    IsBlob = true,
                    ProductionPercentage = 0,
                    CreationTime = DateTime.Now,
                    NotificationExpiresTime = DateTime.Now.AddHours(ExpiresHours)
                };
                int ex_count = dapper.Execute(dapper.GenerateInsertSql(notification, $"{DBName.Main}.Notifications"), notification);
                if (ex_count != 1) throw new Exception("新增通知檢查錯誤 (AddDownloadTask)");
            }
            catch (Exception ex)
            {
                throw new Exception($"AddDownloadTask 錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 產生一般結果通知，沒有進度直接顯示結果
        /// </summary>
        public Guid AddNormalTaskResult(int UserId, string NotificationMessageContent, int NotificationPriority = 1, int ExpiresHours = 24)
        {
            try
            {
                var notification = new Notifications
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = UserId,
                    NotificationMessageContent = NotificationMessageContent,
                    NotificationPriority = NotificationPriority,   // 訊息的優先級，越大越重要，如 1一般，2警告，3錯誤，4立即資訊(如文件下載)
                    NotificationType = "01",    // 01 系統通知, 02 文件產生, 03 系統備份
                    NotificationLink = null,
                    IsLinkNewWindow = false,
                    IsInternalLink = true,
                    IsBlob = false,
                    ProductionPercentage = 100,
                    CreationTime = DateTime.Now,
                    NotificationExpiresTime = DateTime.Now.AddHours(ExpiresHours)
                };
                int ex_count = dapper.Execute(dapper.GenerateInsertSql(notification, $"{DBName.Main}.Notifications"), notification);
                if (ex_count != 1) throw new Exception("新增通知檢查錯誤 (AddNormalTaskResult)");
                return notification.NotificationId;
            }
            catch (Exception ex)
            {
                throw new Exception($"AddNormalTaskResult 錯誤: {ex.Message}");
            }
        }

        public void UpdateProductionPercentage(Guid notificationId, int percentage)
        {
            try
            {
                dapper.Execute($@"UPDATE {DBName.Main}.Notifications SET ProductionPercentage = @percentage WHERE NotificationId = @notificationId", new { notificationId, percentage });
            }
            catch (Exception ex)
            {
                throw new Exception($"UpdateProductionPercentage 錯誤: {ex.Message}");
            }
        }

        public void UpdateErrorMessageContent(Guid notificationId, string errMsg)
        {
            try
            {
                dapper.Execute($@"UPDATE {DBName.Main}.Notifications SET NotificationErrorMessageContent = @NotificationErrorMessageContent, ProductionPercentage = '100', NotificationPriority = '3' WHERE NotificationId = @notificationId", new { notificationId, NotificationErrorMessageContent = errMsg });
            }
            catch (Exception ex)
            {
                throw new Exception($"UpdateErrorMessageContent 錯誤: {ex.Message}");
            }
        }
    }
}

