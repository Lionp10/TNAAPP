namespace TNA.BLL.DTOs
{
    public class EmailDTO
    {
        public string SmtpHost { get; set; } = "localhost";
        public int SmtpPort { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public string? SmtpUser { get; set; }
        public string? SmtpPass { get; set; }
        public string FromName { get; set; } = "TNA ESPORT";
        public string FromEmail { get; set; } = "tna.clan20@gmail.com";
    }
}
