namespace TNA.BLL.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

        // Métodos adicionales para compatibilidad por reflexión (auth controller hace best-effort)
        Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
            => SendEmailAsync(to, subject, body, true, cancellationToken);

        void Send(string to, string subject, string body)
            => SendEmailAsync(to, subject, body, true).GetAwaiter().GetResult();
    }
}
