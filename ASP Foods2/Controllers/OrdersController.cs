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
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            return View(await BuildCheckoutViewModelAsync(userId));
        }

        // POST: Orders/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConfirmed()
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

            var now = DateTime.Now;
            var orders = cartItems.Select(cart => new Order
            {
                ClientId = userId,
                ProductId = cart.ProductId,
                Quantity = cart.Quantity,
                DateAdded = now,
                Status = "Приета"
            }).ToList();

            _context.Orders.AddRange(orders);
            _context.Carts.RemoveRange(_context.Carts.Where(cart => cart.ClientId == userId));

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Поръчката е изпратена успешно! Можеш да следиш статуса и в Моите поръчки.";
                return RedirectToAction(nameof(My));
            }
            catch
            {
                TempData["Error"] = "Възникна проблем при изпращането на поръчката.";
                return View("Create", await BuildCheckoutViewModelAsync(userId));
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
                    _context.Update(order);
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

        private async Task<OrderCheckoutViewModel> BuildCheckoutViewModelAsync(string userId)
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

            return new OrderCheckoutViewModel
            {
                ClientName = user == null
                    ? "Текущ потребител"
                    : string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
                        ? user.Email ?? user.UserName ?? "Текущ потребител"
                        : $"{user.FirstName} {user.LastName}".Trim(),
                ClientEmail = user?.Email ?? string.Empty,
                OrderDate = DateTime.Now,
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
    }
}
