using Dapper;
using GoodSleepEIP.Models;
using Microsoft.Data.SqlClient;
using System.IO;

namespace GoodSleepEIP
{
    /// <summary>
    /// 這裡放一些自維護的工作，例如清理過期通知、附件等，不應該影響 TaskService 的任務調度
    /// </summary>
    public class DataCleanupTaskProcessor : ITaskProcessor
    {
        private readonly IDapperHelper dapper;
        private readonly ILogger<DataCleanupTaskProcessor> logger;
        private readonly IFileService fileService;

        public string TaskType => "DataCleanup";

        public DataCleanupTaskProcessor(IDapperHelper _dapper, ILogger<DataCleanupTaskProcessor> _logger, IFileService _fileService)
        {
            dapper = _dapper;
            logger = _logger;
            fileService = _fileService;
        }

        public async Task<object?> ProcessAsync(Guid taskId, object parameters, CancellationToken token)
        {
            try
            {
                logger.LogInformation("[DataCleanup] 開始清理過期的資料...");

                // 刪除過期的通知
                int deletedNotifications = await dapper.ExecuteAsync($@"
                    DELETE FROM {DBName.Main}.Notifications 
                    WHERE NotificationExpiresTime IS NOT NULL AND NotificationExpiresTime < GETDATE()");

                // 查詢過期附件
                var expiredAttachments = dapper.Query<Attachments>($@"
                    SELECT AttachmentId FROM {DBName.Main}.Attachments 
                    WHERE AttachmentExpiresTime IS NOT NULL AND AttachmentExpiresTime < GETDATE()").ToList();

                int deletedFiles = 0;
                foreach (var expiredAttachment in expiredAttachments)
                {
                    try
                    {
                        // 刪除附件（含實體檔案）
                        fileService.Delete(expiredAttachment.AttachmentId);
                        deletedFiles++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"[DataCleanup] 無法刪除檔案: {expiredAttachment.AttachmentId}");
                    }
                }

                // 刪除過期的附件記錄
                int deletedAttachmentRecords = await dapper.ExecuteAsync($@"
                    DELETE FROM {DBName.Main}.Attachments 
                    WHERE AttachmentExpiresTime IS NOT NULL AND AttachmentExpiresTime < GETDATE()");

                // 清理過期的 Refresh Tokens
                int deletedExpiredTokens = await dapper.ExecuteAsync($@"
                    DELETE FROM {DBName.Main}.UserRefreshTokens 
                    WHERE ExpiryTime < GETDATE()");

                // 刪除已撤銷且超過 3 天的 tokens
                int deletedRevokedTokens = await dapper.ExecuteAsync($@"
                    DELETE FROM {DBName.Main}.UserRefreshTokens 
                    WHERE IsRevoked = 1 
                    AND CreationTime < DATEADD(DAY, -3, GETDATE())");

                logger.LogInformation($"[DataCleanup] 清理完成：刪除通知 {deletedNotifications} 筆，刪除附件記錄 {deletedAttachmentRecords} 筆，刪除檔案 {deletedFiles} 個，刪除過期 tokens {deletedExpiredTokens} 筆，刪除已撤銷 tokens {deletedRevokedTokens} 筆");

                // 長時間運作的不回傳資料，因為接不到、沒意義
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DataCleanup] 清理失敗");
                return null;
            }
        }
    }
}
