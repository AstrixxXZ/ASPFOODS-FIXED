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
    }
}
