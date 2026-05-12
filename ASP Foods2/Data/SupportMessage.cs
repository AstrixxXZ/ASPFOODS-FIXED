using System;

namespace ASP_Foods2.Data
{
    public class SupportMessage
    {
        public int Id { get; set; }
        public string? ClientUserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
}
