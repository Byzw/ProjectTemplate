namespace GoodSleepEIP.Models
{
    public class FileDownloadDto
    {
        public required Stream FileStream { get; set; }
        public required string ContentType { get; set; }
        public required string FileName { get; set; }
    }

    public class Attachments
    {
        public Guid AttachmentId { get; set; } = Guid.NewGuid();
        public required string FileName { get; set; }
        public string? FileExtension { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public required string FilePath { get; set; }
        public required string UploadUser { get; set; }
        public DateTime? UpdateDatetime { get; set; } = DateTime.Now;
        public string? Description { get; set; }
    }
}
