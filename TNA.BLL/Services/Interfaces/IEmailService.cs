namespace TNA.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
        Task SendEmailAsync(string to, string subject, string body, bool isHtml, IEnumerable<Microsoft.AspNetCore.Http.IFormFile>? attachments, CancellationToken cancellationToken = default);
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
            => SendEmailAsync(to, subject, body, true, cancellationToken: cancellationToken);

        void Send(string to, string subject, string body)
            => SendEmailAsync(to, subject, body, true).GetAwaiter().GetResult();
    }
}
