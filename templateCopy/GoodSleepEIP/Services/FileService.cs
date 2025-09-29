using Dapper;
using GoodSleepEIP.Models;

namespace GoodSleepEIP
{
    public interface IFileService
    {
        public Guid Upload(Stream fileStream, Guid attachmentId, string filename, string uploadUser, string extensionFilePath, string description, DateTime? expiresTime = null);
        public Guid Upload(IFormFile file, string uploadUser, string extensionFilePath, string description, string? overwriteFilename = null, DateTime? expiresTime = null);
        Task<FileDownloadDto> Download(Guid attachmentId);
        void Delete(Guid attachmentId);
        void Delete(List<Guid> attachmentIds);
        public Attachments GetFileInformation(Guid attachmentId);
        public List<Attachments> GetFileInformation(List<Guid> attachmentIds);
    }

    public class FileService : IFileService
    {
        private readonly IConfiguration configuration;
        private readonly IDapperHelper dapper;
        private readonly string relativeRootPath;
        private readonly List<string> allowedExtensions;
        private readonly long maxFileSize;

        public FileService(IConfiguration _configuration, IDapperHelper _dapper)
        {
            configuration = _configuration;
            dapper = _dapper;

            var attachmentConfig = configuration.GetSection("Attachment");
            relativeRootPath = attachmentConfig["RelativeRootPath"] ?? "";
            allowedExtensions = attachmentConfig.GetSection("AllowedExtensions").Get<List<string>>() ?? [];
            maxFileSize = attachmentConfig.GetValue<long>("MaxFileSize");
            if (string.IsNullOrEmpty(relativeRootPath) || allowedExtensions == null || allowedExtensions.Count == 0 || maxFileSize <= 0) throw new Exception("Attachment Config setting error.");
        }

        // Filename 很重要(需加上副檔名)，下載時會以此指定content-disposition，且會拆解出副檔名後判斷是否允許
        public Guid Upload(IFormFile file, string uploadUser, string extensionFilePath, string description, string? overwriteFilename = null, DateTime? expiresTime = null)
        {
            if (file == null || file.Length == 0) throw new Exception("No file uploaded.");

            // 轉換 IFormFile -> Stream，並調用 Stream 版本的 Upload
            using var fileStream = file.OpenReadStream();
            return Upload(fileStream, Guid.NewGuid(), overwriteFilename ?? file.FileName, uploadUser, extensionFilePath, description, expiresTime);
        }

        public Guid Upload(Stream fileStream, Guid attachmentId, string filename, string uploadUser, string extensionFilePath, string description, DateTime? expiresTime = null)
        {
            if (fileStream == null || fileStream.Length == 0) throw new Exception("No file uploaded.");
            if (fileStream.Length > maxFileSize) throw new Exception($"File size exceeds limit of {maxFileSize} bytes.");
            if (attachmentId == Guid.Empty) throw new Exception("AttachmentId is required for Stream uploads.");
            if (string.IsNullOrWhiteSpace(filename)) throw new Exception("Filename is required for Stream uploads.");

            // 取得副檔名
            var fileExtension = Path.GetExtension(filename).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension)) throw new Exception("Disallowed file extension.");

            // 生成文件 ID 和路徑
            var filePath = Path.Combine(relativeRootPath, extensionFilePath, attachmentId.ToString() + fileExtension);

            // 確保目錄存在，不存在會嘗試建立
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            // 儲存檔案
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.CopyTo(stream);
            }

            // 嘗試推斷 MIME 類型
            string mimeType = GetMimeType(fileExtension);

            // 儲存文件資訊到資料庫
            var sqlstr = @$"INSERT INTO {DBName.Main}.Attachments (AttachmentId, FileName, FileExtension, MimeType, FileSize, FilePath, UploadUser, UpdateDatetime, AttachmentExpiresTime, Description)
                    VALUES (@AttachmentId, @FileName, @FileExtension, @MimeType, @FileSize, @FilePath, @UploadUser, GETDATE(), @AttachmentExpiresTime, @Description)";

            dapper.Execute(sqlstr, new
            {
                AttachmentId = attachmentId,
                FileName = filename,
                FileExtension = fileExtension,
                MimeType = mimeType,
                FileSize = fileStream.Length,
                FilePath = filePath,
                UploadUser = uploadUser,
                AttachmentExpiresTime = expiresTime,
                Description = description
            });

            return attachmentId;
        }

        public async Task<FileDownloadDto> Download(Guid attachmentId)
        {
            // get data from db
            var sqlstr = $"SELECT * FROM {DBName.Main}.Attachments WHERE AttachmentId = @AttachmentId AND (AttachmentExpiresTime IS NULL OR AttachmentExpiresTime > GETDATE())";
            List<Attachments> attachments = (await dapper.QueryAsync<Attachments>(sqlstr, new { AttachmentId = attachmentId })).ToList();
            if (attachments.Count == 0) throw new Exception("The file does not exist.");
            var attachment = attachments[0];

            if (!File.Exists(attachment.FilePath)) throw new FileNotFoundException("The file was not found on the server.");

            // read stream from file
            var fileStream = new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);

            return new FileDownloadDto
            {
                FileStream = fileStream,
                ContentType = attachment.MimeType ?? "application/octet-stream",
                FileName = attachment.FileName
            };
        }

        // 實體檔案也會刪除
        public void Delete(Guid attachmentId)
        {
            // get data from db
            var sqlstr = $"SELECT * FROM {DBName.Main}.Attachments WHERE AttachmentId = @AttachmentId";
            List<Attachments> attachments = dapper.Query<Attachments>(sqlstr, new { AttachmentId = attachmentId }).ToList();
            if (attachments.Count == 0) throw new Exception("The file does not exist.");
            var attachment = attachments[0];

            dapper.Execute($"DELETE FROM {DBName.Main}.Attachments WHERE AttachmentId = @AttachmentId", new { AttachmentId = attachmentId });

            if (File.Exists(attachment.FilePath))
            {
                try
                {
                    File.Delete(attachment.FilePath);
                }
                catch (IOException ex)
                {
                    throw new ApplicationException("An error occurred while deleting the file.", ex);
                }
            }
        }

        public void Delete(List<Guid> attachmentIds)
        {
            // get data from db
            var sqlstr = $"SELECT * FROM {DBName.Main}.Attachments WHERE AttachmentId = @attachmentIds";
            List<Attachments> attachments = dapper.Query<Attachments>(sqlstr, new { attachmentIds }).ToList();
            if (attachments.Count == 0) throw new Exception("The file does not exist.");

            dapper.Execute($"DELETE FROM {DBName.Main}.Attachments WHERE AttachmentId = @attachmentIds", new { attachmentIds });

            foreach (var attachment in attachments)
            {
                if (File.Exists(attachment.FilePath))
                {
                    try
                    {
                        File.Delete(attachment.FilePath);
                    }
                    catch (IOException ex)
                    {
                        throw new ApplicationException($"An error occurred while deleting the file: {attachment.AttachmentId}", ex);
                    }
                }
            }
        }

        public Attachments GetFileInformation(Guid attachmentId)
        {
            // get data from db
            var sqlstr = $"SELECT * FROM {DBName.Main}.Attachments WHERE AttachmentId = @AttachmentId";
            List<Attachments> attachments = dapper.Query<Attachments>(sqlstr, new { AttachmentId = attachmentId }).ToList();
            if (attachments.Count == 0) throw new Exception("The file does not exist.");
            var attachment = attachments[0];

            if (!File.Exists(attachment.FilePath)) throw new FileNotFoundException("The file was not found on the server.");
            return attachment;
        }

        public List<Attachments> GetFileInformation(List<Guid> attachmentIds)
        {
            // get data from db
            var sqlstr = $"SELECT * FROM {DBName.Main}.Attachments WHERE AttachmentId IN @attachmentIds";
            List<Attachments> attachments = dapper.Query<Attachments>(sqlstr, new { attachmentIds }).ToList();
            if (attachments.Count == 0) throw new Exception("No file found.");

            // 檢查每個附件的文件路徑是否存在
            foreach (var attachment in attachments)
            {
                if (!File.Exists(attachment.FilePath)) throw new FileNotFoundException($"The file '{attachment.AttachmentId}' was not found on the server.");
            }

            return attachments;
        }

        private static string GetMimeType(string fileExtension)
        {
            var mimeTypes = new Dictionary<string, string>
            {
                // 圖片類型
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },
                { ".svg", "image/svg+xml" },
                { ".webp", "image/webp" },

                // 文件類型
                { ".pdf", "application/pdf" },
                { ".txt", "text/plain" },
                { ".csv", "text/csv" },
                { ".xml", "application/xml" },
                { ".json", "application/json" },
                { ".rtf", "application/rtf" },

                // Microsoft Office 舊版
                { ".doc", "application/msword" },
                { ".xls", "application/vnd.ms-excel" },
                { ".ppt", "application/vnd.ms-powerpoint" },

                // Microsoft Office 新版
                { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

                // 音樂與影片
                { ".mp3", "audio/mpeg" },
                { ".wav", "audio/wav" },
                { ".mp4", "video/mp4" },
                { ".mov", "video/quicktime" },

                // 壓縮檔案
                { ".zip", "application/zip" },
                { ".rar", "application/x-rar-compressed" },
                { ".7z", "application/x-7z-compressed" },
                { ".tar", "application/x-tar" },
                { ".gz", "application/gzip" }
            };

            return mimeTypes.TryGetValue(fileExtension, out var mimeType) ? mimeType : "application/octet-stream";
        }
    }
}
