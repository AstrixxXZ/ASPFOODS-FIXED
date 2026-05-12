using System.Linq;
using System.Threading.Tasks;
using ASP_Foods2.Data;
using ASP_Foods2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASP_Foods2.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Client> _userManager;

        public SupportController(ApplicationDbContext context, UserManager<Client> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var userEmail = user.Email ?? string.Empty;
            var replies = await _context.SupportReplies
                .AsNoTracking()
                .Join(
                    _context.SupportMessages.AsNoTracking(),
                    reply => reply.SupportMessageId,
                    supportMessage => supportMessage.Id,
                    (reply, supportMessage) => new { reply, supportMessage })
                .Where(item =>
                    item.reply.RecipientUserId == user.Id ||
                    item.supportMessage.ClientUserId == user.Id ||
                    (!string.IsNullOrWhiteSpace(userEmail) &&
                        (item.reply.RecipientEmail == userEmail ||
                         item.supportMessage.Email == userEmail)))
                .OrderByDescending(item => item.reply.CreatedAt)
                .ToListAsync();

            var unreadReplies = replies
                .Where(item => !item.reply.IsRead)
                .Select(item => item.reply)
                .ToList();

            if (unreadReplies.Count > 0)
            {
                var unreadIds = unreadReplies.Select(reply => reply.Id).ToList();
                var trackedReplies = await _context.SupportReplies
                    .Where(reply => unreadIds.Contains(reply.Id))
                    .ToListAsync();

                foreach (var reply in trackedReplies)
                {
                    reply.IsRead = true;
                    reply.ReadAt = System.DateTime.Now;
                }

                await _context.SaveChangesAsync();

                foreach (var reply in unreadReplies)
                {
                    reply.IsRead = true;
                }
            }

            var model = new SupportInboxPageViewModel
            {
                UnreadCount = unreadReplies.Count,
                Replies = replies.Select(item => new SupportInboxItemViewModel
                {
                    Id = item.reply.Id,
                    Subject = item.supportMessage.Subject,
                    SenderDisplayName = item.reply.SenderDisplayName,
                    Message = item.reply.Message,
                    CreatedAt = item.reply.CreatedAt,
                    IsRead = item.reply.IsRead
                }).ToList()
            };

            return View(model);
        }
    }
}
