using HtmlAgilityPack;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Net.Mail;

namespace GoodSleepEIP
{
    public class EmailConfiguration
    {
        public required string From { get; set; }
        public required string FromName { get; set; }
        public required string SmtpServer { get; set; }
        public required int Port { get; set; }
        public required string secureSocketOptions { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }

    public class Message
    {
        public List<MailboxAddress> To { get; set; } = [];
        public required string Subject { get; set; }
        public required string Content { get; set; }
        public bool IsBodyHtml { get; set; } = false;
        public List<byte[]> Attachment { get; set; } = [];
        public List<string> FileName { get; set; } = [];
    }

    public interface IEmailService
    {
        void SendEmail(Message message);
        void SendEmail(MailMessage message);
        Task SendEmailAsync(Message message);
        Task SendEmailAsync(MailMessage message);
        void NotifyMethod1(string templateFpath, Dictionary<string, string> replacements, string emailSubject, List<string> emailAddress, List<Guid>? attachmentIds = null);
        void NotifyMethod2(string templateFpath, Dictionary<string, string> replacementsNodes, Dictionary<string, List<string[]>> addTableRows, string emailSubject, List<string> emailAddress, List<Guid>? attachmentIds = null);
        void NotifyMethod3(string templateFpath, Dictionary<string, string> replacementsNodes, Dictionary<string, List<string[]>> addTableRows, string emailSubject, List<string> emailAddress, List<Guid>? attachmentIds = null);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly SecureSocketOptions UseSSLOption;
        private readonly EmailConfiguration _emailConfig;
        private readonly IFileService fileService;

        public EmailService(ILogger<EmailService> logger, IConfiguration config, IFileService _fileService)
        {
            _logger = logger;
            _configuration = config; // Assign the config parameter to the _configuration field
            _emailConfig = _configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>() ?? throw new InvalidOperationException("EmailConfiguration section is missing or invalid.");
            fileService = _fileService;

            if (_emailConfig.secureSocketOptions == "StartTls") UseSSLOption = SecureSocketOptions.StartTls;
            else if (_emailConfig.secureSocketOptions == "SslOnConnect") UseSSLOption = SecureSocketOptions.SslOnConnect;
            else if (_emailConfig.secureSocketOptions == "Auto") UseSSLOption = SecureSocketOptions.Auto;
            else UseSSLOption = SecureSocketOptions.None;
        }
        public void SendEmail(Message message)
        {
            var emailMessage = CreateEmailMessage(message);
            Send(emailMessage);
        }

        public void SendEmail(MailMessage message)
        {
            var emailMessage = MimeMessage.CreateFromMailMessage(message);
            emailMessage.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.From));
            Send(emailMessage);
        }

        public async Task SendEmailAsync(Message message)
        {
            var mailMessage = CreateEmailMessage(message);
            await SendAsync(mailMessage);
        }
        public async Task SendEmailAsync(MailMessage message)
        {
            var mailMessage = MimeMessage.CreateFromMailMessage(message);
            mailMessage.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.From));
            await SendAsync(mailMessage);
        }

        private MimeMessage CreateEmailMessage(Message message)
        {
            var emailMessage = new MimeMessage();
            if (message.To == null || message.Subject == null || message.Content == null) return emailMessage;

            TextFormat useType = new TextFormat();
            if (message.IsBodyHtml) useType = TextFormat.Html;
            else useType = TextFormat.Text;

            emailMessage.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;

            // 多檔案新增支援，所有資訊放在對應的 List 裡面
            if (message.Attachment != null && message.FileName != null && message.Attachment.Count == message.FileName.Count)
            {
                var multipart = new Multipart("mixed");
                multipart.Add(new TextPart(useType) { Text = message.Content });

                for (int i = 0; i < message.Attachment.Count(); i++)
                {
                    var attachment = new MimePart() //var attachment = new MimePart(message.MediaType[i], message.MediaSubType[i])
                    {
                        Content = new MimeContent(new MemoryStream(message.Attachment[i]), ContentEncoding.Default),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = Path.GetFileName(message.FileName[i])
                    };
                    multipart.Add(attachment);
                }
                emailMessage.Body = multipart;
            } else emailMessage.Body = new TextPart(useType) { Text = message.Content };

            
            return emailMessage;
        }

        private void Send(MimeMessage mailMessage)
        {
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    client.Connect(_emailConfig.SmtpServer, _emailConfig.Port, UseSSLOption);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    if (UseSSLOption != SecureSocketOptions.None) client.Authenticate(_emailConfig.UserName, _emailConfig.Password);
                    client.Send(mailMessage);
                }
                catch
                {
                    //log an error message or throw an exception or both.
                    throw;
                }
                finally
                {
                    client.Disconnect(true);
                    client.Dispose();
                }
            }
        }

        private async Task SendAsync(MimeMessage mailMessage)
        {
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.Port, UseSSLOption);
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    if (UseSSLOption != SecureSocketOptions.None) await client.AuthenticateAsync(_emailConfig.UserName, _emailConfig.Password);
                    await client.SendAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    //log an error message or throw an exception, or both.
                    _logger.LogError("寄信錯誤: " + ex.Message);
                    throw;
                }
                finally
                {
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
        }

        public async void NotifyMethod1(string templateFpath, Dictionary<string, string> replacements, string emailSubject, List<string> emailAddress, List<Guid>? attachmentIds = null)
        {
            try
            {
                HtmlBody email_work = new HtmlBody();
                using (var mail = new MailMessage())
                {
                    var doc = new HtmlDocument();
                    doc.Load(templateFpath);

                    foreach (var replacement in replacements) email_work.UpdateNodesHtml(doc, replacement.Key, replacement.Value);

                    mail.Subject = emailSubject;
                    mail.Body = doc.DocumentNode.InnerHtml;
                    mail.IsBodyHtml = true;

                    emailAddress = emailAddress.Distinct().ToList();
                    if ((_configuration["OnDevSetting:EmailOverWrite"] ?? "").ToString().Trim() == "")
                    {
                        foreach (var addr in emailAddress) if (addr.Trim().Length >= 5) mail.To.Add(addr.Trim());
                    }
                    else
                    {
                        mail.To.Add((_configuration["OnDevSetting:EmailOverWrite"] ?? "").ToString().Trim());
                    }

                    if (attachmentIds != null)
                    {
                        foreach (var attachmentId in attachmentIds)
                        {
                            var attachment = new System.Net.Mail.Attachment(fileService.GetFileInformation(attachmentId).FilePath);
                            mail.Attachments.Add(attachment);
                        }
                    }

                    await Task.Delay(500);
                    await SendEmailAsync(mail);
                }
            }
            catch
            {
                throw;
            }
        }

        public async void NotifyMethod2(string templateFpath, Dictionary<string, string> replacementsNodes, Dictionary<string, List<string[]>> addTableRows, string emailSubject, List<string> emailAddress, List<Guid>? attachmentIds = null)
        {
            try
            {
                HtmlBody email_work = new HtmlBody();
                using (var mail = new MailMessage())
                {
                    var doc = new HtmlDocument();
                    doc.Load(templateFpath);

                    foreach (var replacement in replacementsNodes)
                    {
                        email_work.UpdateNodesHtml(doc, replacement.Key, replacement.Value);
                    }

                    foreach (var table in addTableRows)
                    {
                        foreach (var row in table.Value)
                        {
                            email_work.AddTableRow(doc, table.Key, row);
                        }
                    }

                    mail.Subject = emailSubject;
                    mail.Body = doc.DocumentNode.InnerHtml;
                    mail.IsBodyHtml = true;

                    emailAddress = emailAddress.Distinct().ToList();
                    if ((_configuration["OnDevSetting:EmailOverWrite"] ?? "").ToString().Trim() == "")
                    {
                        foreach (var addr in emailAddress)
                        {
                            if (addr.Trim().Length >= 5)
                            {
                                mail.To.Add(addr.Trim());
                            }
                        }
                    }
                    else
                    {
                        mail.To.Add((_configuration["OnDevSetting:EmailOverWrite"] ?? "").ToString().Trim());
                    }

                    if (attachmentIds != null)
                    {
                        foreach (var attachmentId in attachmentIds)
                        {
                            var attachment = new System.Net.Mail.Attachment(fileService.GetFileInformation(attachmentId).FilePath);
                            mail.Attachments.Add(attachment);
                        }
                    }

                    await Task.Delay(500);
                    await SendEmailAsync(mail);
                }
            }
            catch
            {
                throw;
            }
        }

        // 同上，改為同步的
        public void NotifyMethod3(string templateFpath, Dictionary<string, string> replacementsNodes, Dictionary<string, List<string[]>> addTableRows, string emailSubject, List<string> emailAddress, List<Guid>? attachmentIds = null)
        {
            try
            {
                HtmlBody email_work = new HtmlBody();
                using (var mail = new MailMessage())
                {
                    var doc = new HtmlDocument();
                    doc.Load(templateFpath);

                    foreach (var replacement in replacementsNodes)
                    {
                        email_work.UpdateNodesHtml(doc, replacement.Key, replacement.Value);
                    }

                    foreach (var table in addTableRows)
                    {
                        foreach (var row in table.Value)
                        {
                            email_work.AddTableRow(doc, table.Key, row);
                        }
                    }

                    mail.Subject = emailSubject;
                    mail.Body = doc.DocumentNode.InnerHtml;
                    mail.IsBodyHtml = true;

                    emailAddress = emailAddress.Distinct().ToList();
                    if ((_configuration["OnDevSetting:EmailOverWrite"] ?? "").ToString().Trim() == "")
                    {
                        foreach (var addr in emailAddress)
                        {
                            if (addr.Trim().Length >= 5)
                            {
                                mail.To.Add(addr.Trim());
                            }
                        }
                    }
                    else
                    {
                        mail.To.Add(_configuration["OnDevSetting:EmailOverWrite"] ?? "".ToString().Trim());
                    }

                    if (attachmentIds != null)
                    {
                        foreach (var attachmentId in attachmentIds)
                        {
                            var attachment = new System.Net.Mail.Attachment(fileService.GetFileInformation(attachmentId).FilePath);
                            mail.Attachments.Add(attachment);
                        }
                    }

                    SendEmail(mail);
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
