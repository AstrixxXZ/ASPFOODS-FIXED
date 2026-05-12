using System;
using System.Collections.Generic;

namespace ASP_Foods2.Models
{
    public class SupportInboxPageViewModel
    {
        public int UnreadCount { get; set; }
        public List<SupportInboxItemViewModel> Replies { get; set; } = new();
    }

    public class SupportInboxItemViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
