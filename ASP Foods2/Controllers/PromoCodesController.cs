using System;
using System.Linq;
using System.Threading.Tasks;
using ASP_Foods2.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASP_Foods2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromoCodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromoCodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var promoCodes = await _context.PromoCodes
                .AsNoTracking()
                .OrderByDescending(promoCode => promoCode.CreatedAt)
                .ToListAsync();

            return View(promoCodes);
        }

        public IActionResult Create()
        {
            return View(new PromoCode
            {
                IsActive = true,
                ValidFrom = DateTime.Now,
                ValidTo = DateTime.Now.AddMonths(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromoCode promoCode)
        {
            promoCode.Code = NormalizePromoCode(promoCode.Code);
            if (await PromoCodeExistsAsync(promoCode.Code))
            {
                ModelState.AddModelError(nameof(PromoCode.Code), "Този промокод вече съществува.");
            }

            if (!ValidatePromoCodeDates(promoCode))
            {
                ModelState.AddModelError(nameof(PromoCode.ValidTo), "Крайната дата трябва да е след началната.");
            }

            if (!ModelState.IsValid)
            {
                return View(promoCode);
            }

            promoCode.CreatedAt = DateTime.Now;
            _context.PromoCodes.Add(promoCode);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Промокодът е създаден успешно.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode == null)
            {
                return NotFound();
            }

            return View(promoCode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromoCode promoCode)
        {
            if (id != promoCode.Id)
            {
                return NotFound();
            }

            promoCode.Code = NormalizePromoCode(promoCode.Code);
            if (await _context.PromoCodes.AnyAsync(existingPromoCode =>
                existingPromoCode.Id != promoCode.Id && existingPromoCode.Code == promoCode.Code))
            {
                ModelState.AddModelError(nameof(PromoCode.Code), "Този промокод вече съществува.");
            }

            if (!ValidatePromoCodeDates(promoCode))
            {
                ModelState.AddModelError(nameof(PromoCode.ValidTo), "Крайната дата трябва да е след началната.");
            }

            if (!ModelState.IsValid)
            {
                return View(promoCode);
            }

            try
            {
                _context.Update(promoCode);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.PromoCodes.AnyAsync(existingPromoCode => existingPromoCode.Id == promoCode.Id))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["Success"] = "Промокодът е обновен успешно.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var promoCode = await _context.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(existingPromoCode => existingPromoCode.Id == id);

            if (promoCode == null)
            {
                return NotFound();
            }

            return View(promoCode);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promoCode = await _context.PromoCodes.FindAsync(id);
            if (promoCode == null)
            {
                return NotFound();
            }

            _context.PromoCodes.Remove(promoCode);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Промокодът е изтрит успешно.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> PromoCodeExistsAsync(string code)
        {
            return await _context.PromoCodes.AnyAsync(promoCode => promoCode.Code == code);
        }

        private static string NormalizePromoCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool ValidatePromoCodeDates(PromoCode promoCode)
        {
            if (promoCode.ValidFrom.HasValue && promoCode.ValidTo.HasValue)
            {
                return promoCode.ValidTo.Value >= promoCode.ValidFrom.Value;
            }

            return true;
        }
    }
}
