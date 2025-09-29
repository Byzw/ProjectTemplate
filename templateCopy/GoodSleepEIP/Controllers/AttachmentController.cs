using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.AspNetCore.Authorization;

namespace GoodSleepEIP.Controllers
{
    [Authorize]
    [Route("api/attachment")]
    [ApiController]
    public partial class AttachmentController : ControllerBase
    {
        private readonly IFileService fileService;

        public AttachmentController(IFileService fileService)
            => (this.fileService) = (fileService);

        // 平時直接其他控制項呼叫 DI 上傳檔案，這裡不會用到
        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file, string uploadPersonId, string extensionFilePath, string description)
        {
            try
            {
                var attachmentId = fileService.Upload(file, uploadPersonId, extensionFilePath, description);
                return Ok(new { AttachmentId = attachmentId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("download/{attachmentId}")]
        public async Task<IActionResult> DownloadFileInline(Guid attachmentId)
        {
            try
            {
                var fileDownloadDto = await fileService.Download(attachmentId);

                var fileResult = File(fileDownloadDto.FileStream, fileDownloadDto.ContentType, enableRangeProcessing: true);
                fileResult.FileDownloadName = fileDownloadDto.FileName; // 設定檔名，ASP.NET Core 會自動加上 Content-Disposition

                return fileResult;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 下載檔案
        [HttpGet("ddownload/{attachmentId}")]
        public async Task<IActionResult> DownloadFileDirect(Guid attachmentId)
        {
            try
            {
                var fileDownloadDto = await fileService.Download(attachmentId);
                return File(fileDownloadDto.FileStream, fileDownloadDto.ContentType, fileDownloadDto.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetAttachmentInfo/{attachmentId}")]
        public IActionResult GetAttachmentInfo(Guid attachmentId)
        {
            try
            {
                var attachment = fileService.GetFileInformation(attachmentId);
                return Ok(attachment);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("GetAttachmentsInfo")]
        public IActionResult GetAttachmentsInfo([FromBody] List<Guid> attachmentIds)
        {
            try
            {
                var attachments = fileService.GetFileInformation(attachmentIds);
                return Ok(attachments);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("delete/{attachmentId}")]
        public IActionResult DeleteFile(Guid attachmentId)
        {
            try
            {
                fileService.Delete(attachmentId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
