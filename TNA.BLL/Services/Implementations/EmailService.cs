using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;

namespace TNA.BLL.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailDTO _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailDTO> options, ILogger<EmailService> logger)
        {
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
            => await SendEmailAsync(to, subject, body, isHtml, (IEnumerable<IFormFile>?)null, cancellationToken).ConfigureAwait(false);

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml, IEnumerable<IFormFile>? attachments, CancellationToken cancellationToken = default)
        {
            using var msg = new MailMessage();
            msg.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            msg.To.Add(new MailAddress(to));
            msg.Subject = subject;
            msg.Body = body;
            msg.IsBodyHtml = isHtml;

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    try
                    {
                        if (file == null || file.Length == 0 || string.IsNullOrWhiteSpace(file.FileName)) continue;
                        var stream = file.OpenReadStream();
                        var attachment = new Attachment(stream, file.FileName, string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
                        msg.Attachments.Add(attachment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo adjuntar fichero {FileName}", file?.FileName);
                    }
                }
            }

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = string.IsNullOrWhiteSpace(_settings.SmtpUser)
            };

            if (!string.IsNullOrWhiteSpace(_settings.SmtpUser))
            {
                client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass);
            }

            try
            {
                await client.SendMailAsync(msg, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Email enviado a {To} (subject: {Subject})", to, subject);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Envio de email cancelado para {To}", to);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email a {To}", to);
                throw;
            }
        }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
            => SendEmailAsync(to, subject, body, true, cancellationToken: cancellationToken);

        public void Send(string to, string subject, string body)
            => SendEmailAsync(to, subject, body, true).GetAwaiter().GetResult();
    }
}
