using System;

namespace ASP_Foods2.Data
{
    public class SupportReply
    {
        public int Id { get; set; }
        public int SupportMessageId { get; set; }
        public string? RecipientUserId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = "SuperFoodsBG Support";
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
