using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ASP_Foods2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ASP_Foods2.Data;

namespace ASP_Foods2.Controllers
{
    public class OrdersController : Controller
    {
        private static readonly string[] OrderStatuses =
        {
            "Приета",
            "Обработва се",
            "Изпратена",
            "Доставена",
            "Отказана"
        };

        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Orders.Include(o => o.Clients).Include(o => o.Products);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Orders/My
        [Authorize]
        public async Task<IActionResult> My()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(order => order.Products)
                .Where(order => order.ClientId == userId)
                .OrderByDescending(order => order.DateAdded)
                .ToListAsync();

            return View(orders);
        }

        // GET: Orders/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Clients)
                .Include(o => o.Products)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        [Authorize]
        public async Task<IActionResult> Create(string? promoCode = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            return View(await BuildCheckoutViewModelAsync(userId, promoCode));
        }

        // POST: Orders/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConfirmed(string? promoCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var cartItems = await _context.Carts
                .AsNoTracking()
                .Include(cart => cart.Products)
                .Where(cart => cart.ClientId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Количката е празна и няма какво да поръчаме.";
                return RedirectToAction("Index", "Carts");
            }

            var subtotalAmount = cartItems.Sum(cart => (cart.Products?.Price ?? 0m) * cart.Quantity);
            var promoEvaluation = await EvaluatePromoCodeAsync(promoCode, subtotalAmount);
            if (!string.IsNullOrWhiteSpace(promoCode) && !promoEvaluation.IsValid)
            {
                return View("Create", await BuildCheckoutViewModelAsync(userId, promoCode, promoEvaluation.Message, true));
            }

            var now = DateTime.Now;
            var discountPercent = promoEvaluation.IsValid ? promoEvaluation.DiscountPercent : 0m;
            var normalizedPromoCode = promoEvaluation.IsValid ? promoEvaluation.AppliedCode : string.Empty;
            var allocatedDiscounts = AllocateLineDiscounts(cartItems, discountPercent, promoEvaluation.DiscountAmount);
            var orders = cartItems.Select((cart, index) =>
            {
                var unitPrice = cart.Products?.Price ?? 0m;

                return new Order
                {
                    ClientId = userId,
                    ProductId = cart.ProductId,
                    Quantity = cart.Quantity,
                    DateAdded = now,
                    Status = "Приета",
                    UnitPrice = unitPrice,
                    PromoCode = string.IsNullOrWhiteSpace(normalizedPromoCode) ? null : normalizedPromoCode,
                    DiscountPercent = discountPercent,
                    DiscountAmount = allocatedDiscounts[index]
                };
            }).ToList();

            _context.Orders.AddRange(orders);
            _context.Carts.RemoveRange(_context.Carts.Where(cart => cart.ClientId == userId));
            if (promoEvaluation.IsValid && promoEvaluation.PromoCode != null)
            {
                promoEvaluation.PromoCode.UsedCount += 1;
                _context.PromoCodes.Update(promoEvaluation.PromoCode);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Поръчката е изпратена успешно! Можеш да следиш статуса и в Моите поръчки.";
                return RedirectToAction(nameof(My));
            }
            catch
            {
                TempData["Error"] = "Възникна проблем при изпращането на поръчката.";
                return View("Create", await BuildCheckoutViewModelAsync(userId, promoCode));
            }
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            order.Status = NormalizeOrderStatus(order.Status);
            PopulateDropdowns(order);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,ProductId,Quantity,DateAdded,Status")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            order.Status = NormalizeOrderStatus(order.Status);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _context.Orders.FindAsync(id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    existingOrder.ClientId = order.ClientId;
                    existingOrder.ProductId = order.ProductId;
                    existingOrder.Quantity = order.Quantity;
                    existingOrder.DateAdded = order.DateAdded;
                    existingOrder.Status = order.Status;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(order);
            return View(order);
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Clients)
                .Include(o => o.Products)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        private void PopulateDropdowns(Order? order = null)
        {
            var clients = _context.Users
                .AsNoTracking()
                .ToList()
                .Select(user => new
                {
                    user.Id,
                    DisplayName = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                        ? user.Email ?? user.UserName ?? user.Id
                        : $"{user.FirstName} {user.LastName}".Trim()
                })
                .OrderBy(user => user.DisplayName)
                .ToList();

            var products = _context.Products
                .AsNoTracking()
                .ToList()
                .Select(product => new
                {
                    product.Id,
                    DisplayName = string.IsNullOrWhiteSpace(product.CatalogId)
                        ? product.Name
                        : $"{product.Name} ({product.CatalogId})"
                })
                .OrderBy(product => product.DisplayName)
                .ToList();

            ViewData["ClientId"] = new SelectList(clients, "Id", "DisplayName", order?.ClientId);
            ViewData["ProductId"] = new SelectList(products, "Id", "DisplayName", order?.ProductId);
            ViewData["Status"] = new SelectList(OrderStatuses, NormalizeOrderStatus(order?.Status));
        }

        private static string NormalizeOrderStatus(string? status)
        {
            var normalizedStatus = (status ?? string.Empty).Trim();

            return OrderStatuses.FirstOrDefault(orderStatus =>
                string.Equals(orderStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase)) ?? "Приета";
        }

        private async Task<OrderCheckoutViewModel> BuildCheckoutViewModelAsync(
            string userId,
            string? promoCodeInput = null,
            string? forcedPromoMessage = null,
            bool forcedPromoError = false)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(client => client.Id == userId);

            var cartItems = await _context.Carts
                .AsNoTracking()
                .Include(cart => cart.Products)
                .Where(cart => cart.ClientId == userId)
                .OrderByDescending(cart => cart.DateAdded)
                .ToListAsync();

            var subtotalAmount = cartItems.Sum(cart => (cart.Products?.Price ?? 0m) * cart.Quantity);
            var promoEvaluation = await EvaluatePromoCodeAsync(promoCodeInput, subtotalAmount);

            return new OrderCheckoutViewModel
            {
                ClientName = user == null
                    ? "Текущ потребител"
                    : string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                        ? user.Email ?? user.UserName ?? "Текущ потребител"
                        : $"{user.FirstName} {user.LastName}".Trim(),
                ClientEmail = user?.Email ?? string.Empty,
                OrderDate = DateTime.Now,
                PromoCodeInput = promoCodeInput?.Trim().ToUpperInvariant() ?? string.Empty,
                AppliedPromoCode = promoEvaluation.IsValid ? promoEvaluation.AppliedCode : string.Empty,
                PromoDescription = promoEvaluation.IsValid ? promoEvaluation.Description : string.Empty,
                PromoMessage = !string.IsNullOrWhiteSpace(forcedPromoMessage)
                    ? forcedPromoMessage
                    : promoEvaluation.Message,
                PromoMessageIsError = !string.IsNullOrWhiteSpace(forcedPromoMessage)
                    ? forcedPromoError
                    : !promoEvaluation.IsValid && !string.IsNullOrWhiteSpace(promoCodeInput),
                DiscountPercent = promoEvaluation.IsValid ? promoEvaluation.DiscountPercent : 0m,
                DiscountAmount = promoEvaluation.IsValid ? promoEvaluation.DiscountAmount : 0m,
                Items = cartItems.Select(cart => new OrderCheckoutItemViewModel
                {
                    CartId = cart.Id,
                    ProductId = cart.ProductId,
                    ProductName = cart.Products?.Name ?? "Продукт",
                    CatalogId = cart.Products?.CatalogId ?? string.Empty,
                    ImageUrl = cart.Products?.ImageUrl,
                    UnitPrice = cart.Products?.Price ?? 0m,
                    Quantity = cart.Quantity
                }).ToList()
            };
        }

        private async Task<PromoCodeEvaluationResult> EvaluatePromoCodeAsync(string? promoCodeInput, decimal subtotalAmount)
        {
            var normalizedPromoCode = (promoCodeInput ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedPromoCode))
            {
                return PromoCodeEvaluationResult.Empty;
            }

            var promoCode = await _context.PromoCodes
                .FirstOrDefaultAsync(existingPromoCode => existingPromoCode.Code == normalizedPromoCode);

            if (promoCode == null)
            {
                return PromoCodeEvaluationResult.Invalid("Няма такъв промокод.");
            }

            if (!promoCode.IsActive)
            {
                return PromoCodeEvaluationResult.Invalid("Промокодът е изключен.");
            }

            if (promoCode.ValidFrom.HasValue && promoCode.ValidFrom.Value > DateTime.Now)
            {
                return PromoCodeEvaluationResult.Invalid("Промокодът все още не е активен.");
            }

            if (promoCode.ValidTo.HasValue && promoCode.ValidTo.Value < DateTime.Now)
            {
                return PromoCodeEvaluationResult.Invalid("Промокодът е изтекъл.");
            }

            if (promoCode.UsageLimit.HasValue && promoCode.UsedCount >= promoCode.UsageLimit.Value)
            {
                return PromoCodeEvaluationResult.Invalid("Промокодът е изчерпан.");
            }

            if (subtotalAmount < promoCode.MinimumOrderAmount)
            {
                return PromoCodeEvaluationResult.Invalid($"Промокодът важи за поръчки над {promoCode.MinimumOrderAmount:0.00} лв.");
            }

            var discountAmount = Math.Round(subtotalAmount * promoCode.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

            return PromoCodeEvaluationResult.Valid(
                promoCode,
                normalizedPromoCode,
                promoCode.Description,
                promoCode.DiscountPercent,
                discountAmount,
                $"Промокодът {normalizedPromoCode} е приложен успешно.");
        }

        private static List<decimal> AllocateLineDiscounts(IEnumerable<Cart> cartItems, decimal discountPercent, decimal totalDiscountAmount)
        {
            var items = cartItems.ToList();
            if (!items.Any() || discountPercent <= 0m || totalDiscountAmount <= 0m)
            {
                return items.Select(_ => 0m).ToList();
            }

            var discounts = new List<decimal>(items.Count);
            var allocatedAmount = 0m;

            for (var index = 0; index < items.Count; index++)
            {
                if (index == items.Count - 1)
                {
                    discounts.Add(Math.Max(totalDiscountAmount - allocatedAmount, 0m));
                    break;
                }

                var item = items[index];
                var lineSubtotal = (item.Products?.Price ?? 0m) * item.Quantity;
                var lineDiscount = Math.Round(
                    lineSubtotal * discountPercent / 100m,
                    2,
                    MidpointRounding.AwayFromZero);

                discounts.Add(lineDiscount);
                allocatedAmount += lineDiscount;
            }

            return discounts;
        }

        private sealed record PromoCodeEvaluationResult(
            bool IsValid,
            PromoCode? PromoCode,
            string AppliedCode,
            string Description,
            decimal DiscountPercent,
            decimal DiscountAmount,
            string Message)
        {
            public static PromoCodeEvaluationResult Empty =>
                new(false, null, string.Empty, string.Empty, 0m, 0m, string.Empty);

            public static PromoCodeEvaluationResult Invalid(string message) =>
                new(false, null, string.Empty, string.Empty, 0m, 0m, message);

            public static PromoCodeEvaluationResult Valid(
                PromoCode promoCode,
                string appliedCode,
                string description,
                decimal discountPercent,
                decimal discountAmount,
                string message) =>
                new(true, promoCode, appliedCode, description, discountPercent, discountAmount, message);
        }
    }
}
