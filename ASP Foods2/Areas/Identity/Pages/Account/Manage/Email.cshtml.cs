#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using ASP_Foods2.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace ASP_Foods2.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<Client> _userManager;
        private readonly SignInManager<Client> _signInManager;

        public EmailModel(
            UserManager<Client> userManager,
            SignInManager<Client> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Имейлът е задължителен")]
            [EmailAddress(ErrorMessage = "Невалиден имейл адрес")]
            [Display(Name = "Нов имейл")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(Client user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Потребителят с ID '{_userManager.GetUserId(User)}' не може да бъде зареден.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Потребителят с ID '{_userManager.GetUserId(User)}' не може да бъде зареден.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);
                if (!setEmailResult.Succeeded)
                {
                    StatusMessage = "Грешка при промяна на имейла.";
                    return RedirectToPage();
                }

                await _userManager.SetUserNameAsync(user, Input.NewEmail);
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Имейлът е променен успешно!";
                return RedirectToPage();
            }

            StatusMessage = "Имейлът не е променен.";
            return RedirectToPage();
        }
    }
}
