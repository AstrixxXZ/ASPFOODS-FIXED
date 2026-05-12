using System;
using System.Collections.Generic;

namespace ASP_Foods2.Models
{
    public class AdminSupportMessagesPageViewModel
    {
        public int TotalCount { get; set; }
        public List<AdminSupportMessageListItemViewModel> Messages { get; set; } = new();
    }

    public class AdminSupportMessageListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
        public string ReplyMessage { get; set; } = string.Empty;
        public bool CanReply { get; set; } = true;
        public string ReplyBlockedReason { get; set; } = string.Empty;
        public List<AdminSupportReplyItemViewModel> Replies { get; set; } = new();
    }

    public class AdminSupportReplyItemViewModel
    {
        public int Id { get; set; }
        public string SenderDisplayName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
