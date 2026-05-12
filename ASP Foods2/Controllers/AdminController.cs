using System;
using System.Collections.Generic;
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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string ResetPasswordValue = "TestPass1";
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Client> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<Client> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var recentOrders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.Clients)
                .Include(order => order.Products)
                .OrderByDescending(order => order.DateAdded)
                .Take(6)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                ProductCount = await _context.Products.CountAsync(),
                BrandCount = await _context.Brands.CountAsync(),
                OrderCount = await _context.Orders.CountAsync(),
                CategoryCount = await _context.Categories.CountAsync(),
                SupportMessageCount = await _context.SupportMessages.CountAsync(),
                TotalOrderedQuantity = await _context.Orders.SumAsync(order => (int?)order.Quantity) ?? 0,
                RecentOrders = recentOrders.Select(order => new AdminRecentOrderViewModel
                {
                    Id = order.Id,
                    ClientName = GetClientName(order.Clients),
                    ProductName = order.Products?.Name ?? "Продукт",
                    ProductCatalogId = order.Products?.CatalogId ?? string.Empty,
                    Quantity = order.Quantity,
                    DateAdded = order.DateAdded
                }).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> SupportMessages()
        {
            var messages = await _context.SupportMessages
                .AsNoTracking()
                .OrderByDescending(message => message.ReceivedAt)
                .ToListAsync();

            var supportMessageIds = messages.Select(message => message.Id).ToList();
            var replies = await _context.SupportReplies
                .AsNoTracking()
                .Where(reply => supportMessageIds.Contains(reply.SupportMessageId))
                .OrderBy(reply => reply.CreatedAt)
                .ToListAsync();
            var supportUsers = await _userManager.Users
                .AsNoTracking()
                .Where(user =>
                    messages.Select(message => message.ClientUserId).Contains(user.Id) ||
                    messages.Select(message => message.Email).Contains(user.Email ?? string.Empty))
                .ToListAsync();
            var supportUsersById = supportUsers
                .Where(user => !string.IsNullOrWhiteSpace(user.Id))
                .ToDictionary(user => user.Id, user => user);
            var supportUsersByEmail = supportUsers
                .Where(user => !string.IsNullOrWhiteSpace(user.Email))
                .GroupBy(user => user.Email!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var adminUserIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var user in supportUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    adminUserIds.Add(user.Id);
                }
            }

            var model = new AdminSupportMessagesPageViewModel
            {
                TotalCount = messages.Count,
                Messages = messages.Select(message => new AdminSupportMessageListItemViewModel
                {
                    CanReply = !IsAdminRecipient(message, supportUsersById, supportUsersByEmail, adminUserIds),
                    ReplyBlockedReason = IsAdminRecipient(message, supportUsersById, supportUsersByEmail, adminUserIds)
                        ? "Не може да изпращаш support отговор към администраторски акаунт."
                        : string.Empty,
                    Id = message.Id,
                    Name = message.Name,
                    Email = message.Email,
                    Subject = message.Subject,
                    Message = message.Message,
                    IpAddress = message.IpAddress,
                    UserAgent = message.UserAgent,
                    FileName = message.FileName,
                    ReceivedAt = message.ReceivedAt,
                    Replies = replies
                        .Where(reply => reply.SupportMessageId == message.Id)
                        .Select(reply => new AdminSupportReplyItemViewModel
                        {
                            Id = reply.Id,
                            SenderDisplayName = reply.SenderDisplayName,
                            Message = reply.Message,
                            CreatedAt = reply.CreatedAt,
                            IsRead = reply.IsRead
                        })
                        .ToList()
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyToSupportMessage(int id, string replyMessage)
        {
            replyMessage = (replyMessage ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(replyMessage))
            {
                TempData["Error"] = "Напиши съобщение за отговор.";
                return RedirectToAction(nameof(SupportMessages));
            }

            var supportMessage = await _context.SupportMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(message => message.Id == id);

            if (supportMessage == null)
            {
                return NotFound();
            }

            var recipientUserId = supportMessage.ClientUserId;
            if (string.IsNullOrWhiteSpace(recipientUserId))
            {
                var user = await _userManager.FindByEmailAsync(supportMessage.Email);
                recipientUserId = user?.Id;
            }

            if (await IsAdminRecipientAsync(supportMessage, recipientUserId))
            {
                TempData["Error"] = "Не може да изпращаш support отговор към администраторски акаунт.";
                return RedirectToAction(nameof(SupportMessages));
            }

            var reply = new SupportReply
            {
                SupportMessageId = supportMessage.Id,
                RecipientUserId = recipientUserId,
                RecipientEmail = supportMessage.Email,
                SenderDisplayName = "SuperFoodsBG Support",
                Message = replyMessage,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.SupportReplies.Add(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Отговорът беше изпратен и е видим в пощата на потребителя.";
            return RedirectToAction(nameof(SupportMessages));
        }

        public async Task<IActionResult> Profiles()
        {
            var orderCounts = await _context.Orders
                .AsNoTracking()
                .GroupBy(order => order.ClientId)
                .Select(group => new { ClientId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(group => group.ClientId, group => group.Count);

            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(user => user.UserName)
                .ToListAsync();

            var model = new List<AdminProfileListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new AdminProfileListItemViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = GetClientName(user),
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Roles = roles.Any() ? string.Join(", ", roles) : "Без роля",
                    OrderCount = orderCounts.TryGetValue(user.Id, out var count) ? count : 0
                });
            }

            return View(model);
        }

        public async Task<IActionResult> ProfileDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new AdminProfileDetailsViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Roles = roles.Any() ? string.Join(", ", roles) : "Без роля",
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                OrderCount = await _context.Orders.CountAsync(order => order.ClientId == user.Id)
            };

            return View(model);
        }

        public async Task<IActionResult> EditProfile(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new AdminProfileEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                EmailConfirmed = user.EmailConfirmed
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string id, AdminProfileEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FirstName = model.FirstName?.Trim() ?? string.Empty;
            user.LastName = model.LastName?.Trim() ?? string.Empty;
            user.EmailConfirmed = model.EmailConfirmed;

            var updateResult = await ApplyIdentityChangeAsync(user, model.UserName, model.Email, model.PhoneNumber);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                return View(model);
            }

            TempData["Success"] = "Профилът е обновен успешно.";
            return RedirectToAction(nameof(ProfileDetails), new { id = user.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetProfilePassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, ResetPasswordValue);
            await _userManager.UpdateSecurityStampAsync(user);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                TempData["Error"] = "Паролата не беше занулена.";
                return RedirectToAction(nameof(ProfileDetails), new { id });
            }

            TempData["Success"] = $"Паролата е занулена успешно. Новата парола е {ResetPasswordValue}.";
            return RedirectToAction(nameof(ProfileDetails), new { id });
        }

        private async Task<IdentityResult> ApplyIdentityChangeAsync(Client user, string userName, string email, string? phoneNumber)
        {
            var trimmedUserName = userName.Trim();
            var trimmedEmail = email.Trim();
            var trimmedPhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

            if (!string.Equals(user.UserName, trimmedUserName, StringComparison.Ordinal))
            {
                var userNameResult = await _userManager.SetUserNameAsync(user, trimmedUserName);
                if (!userNameResult.Succeeded)
                {
                    return userNameResult;
                }
            }

            if (!string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(user, trimmedEmail);
                if (!emailResult.Succeeded)
                {
                    return emailResult;
                }
            }

            if (!string.Equals(user.PhoneNumber, trimmedPhoneNumber, StringComparison.Ordinal))
            {
                var phoneResult = await _userManager.SetPhoneNumberAsync(user, trimmedPhoneNumber);
                if (!phoneResult.Succeeded)
                {
                    return phoneResult;
                }
            }

            return await _userManager.UpdateAsync(user);
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private static string GetClientName(Client? client)
        {
            if (client == null)
            {
                return "Непознат клиент";
            }

            if (!string.IsNullOrWhiteSpace(client.FirstName) || !string.IsNullOrWhiteSpace(client.LastName))
            {
                return $"{client.FirstName} {client.LastName}".Trim();
            }

            return client.Email ?? client.UserName ?? "Клиент";
        }

        private async Task<bool> IsAdminRecipientAsync(SupportMessage supportMessage, string? recipientUserId)
        {
            Client? recipientUser = null;

            if (!string.IsNullOrWhiteSpace(recipientUserId))
            {
                recipientUser = await _userManager.FindByIdAsync(recipientUserId);
            }

            if (recipientUser == null && !string.IsNullOrWhiteSpace(supportMessage.Email))
            {
                recipientUser = await _userManager.FindByEmailAsync(supportMessage.Email);
            }

            if (recipientUser == null)
            {
                return false;
            }

            return await _userManager.IsInRoleAsync(recipientUser, "Admin");
        }

        private static bool IsAdminRecipient(
            SupportMessage supportMessage,
            IReadOnlyDictionary<string, Client> supportUsersById,
            IReadOnlyDictionary<string, Client> supportUsersByEmail,
            ISet<string> adminUserIds)
        {
            if (!string.IsNullOrWhiteSpace(supportMessage.ClientUserId) &&
                adminUserIds.Contains(supportMessage.ClientUserId))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(supportMessage.Email) &&
                supportUsersByEmail.TryGetValue(supportMessage.Email, out var userByEmail) &&
                adminUserIds.Contains(userByEmail.Id))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(supportMessage.ClientUserId) &&
                   supportUsersById.TryGetValue(supportMessage.ClientUserId, out var userById) &&
                   adminUserIds.Contains(userById.Id);
        }
    }
}
