using Dapper;
using GoodSleepEIP.Models;
using Microsoft.Data.SqlClient;
using System.IO;
using System.IO.Compression;

namespace GoodSleepEIP
{
    public class DatabaseBackupTaskProcessor : ITaskProcessor
    {
        private readonly ILogger<DatabaseBackupTaskProcessor> logger;
        private readonly IConfiguration configuration;
        private readonly string connectionString;
        private readonly IDapperHelper dapper;
        private readonly IFileService fileService;
        private readonly NotificationService notificationService;

        public string TaskType => "DatabaseBackup";

        public DatabaseBackupTaskProcessor(
            ILogger<DatabaseBackupTaskProcessor> _logger,
            IConfiguration _configuration,
            IDapperHelper _dapper,
            IFileService _fileService,
            NotificationService _notificationService)
        {
            logger = _logger;
            configuration = _configuration;
            connectionString = configuration["DapperHelperOptions:ConnectionString"] ?? "";
            dapper = _dapper;
            fileService = _fileService;
            notificationService = _notificationService;
        }

        /// <summary>
        /// 執行 SQL Server 備份並回傳上傳的附件 ID
        /// </summary>
        public async Task<object?> ProcessAsync(Guid taskId, object parameters, CancellationToken token)
        {
            try
            {
                logger.LogInformation($"[DatabaseBackup] 開始資料庫備份，TaskId: {taskId}");

                // 確保備份暫存目錄存在
                string backupDir = Path.Combine(Directory.GetCurrentDirectory(), "BackupTmp");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

                string backupFileName = $"backup_{taskId}.bak";
                string backupFilePath = Path.Combine(backupDir, backupFileName);
                Guid uploadResult;

                notificationService.UpdateProductionPercentage(taskId, 10);

                // 執行 SQL Server 備份
                using (var connection = new SqlConnection(connectionString))
                {
                    string backupDB = configuration["DBName:Main"] ?? "";
                    string backupSql = $"BACKUP DATABASE {backupDB} TO DISK = '{backupFilePath}' WITH FORMAT";
                    await connection.ExecuteAsync(backupSql);
                }

                notificationService.UpdateProductionPercentage(taskId, 85);

                // 壓縮備份檔案到記憶體
                using (var zipStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                    {
                        archive.CreateEntryFromFile(backupFilePath, backupFileName);
                    }

                    // 重新定位 Stream 以準備上傳
                    zipStream.Position = 0;

                    // 上傳 ZIP 檔案，設定 24 小時後過期
                    uploadResult = fileService.Upload(
                        zipStream,
                        taskId,
                        $"sqlserver_backup_{DateTime.Now:yyyyMMdd-HHmm}.zip",
                        "System",
                        "DatabaseBackup",
                        "SQL Server 資料庫備份",
                        DateTime.Now.AddHours(24)
                    );
                }

                // 刪除原始 .bak 檔案
                File.Delete(backupFilePath);

                logger.LogInformation($"[DatabaseBackup] 備份完成: {backupFilePath}");
                notificationService.UpdateProductionPercentage(taskId, 100);

                // 長時間運作的不回傳資料，因為接不到、沒意義
                return null;
            }
            catch (Exception ex)
            {
                var msg = $"[DatabaseBackup] 備份失敗: {ex.Message}";
                notificationService.UpdateErrorMessageContent(taskId, msg);
                logger.LogError(msg);
                return null;
            }
        }
    }
}
