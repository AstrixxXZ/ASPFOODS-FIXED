using System.ComponentModel.DataAnnotations;

namespace ASP_Foods2.Models
{
    public class AdminProfileListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }

    public class AdminProfileDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Roles { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public int OrderCount { get; set; }
    }

    public class AdminProfileEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Потребителското име е задължително.")]
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имейлът е задължителен.")]
        [EmailAddress(ErrorMessage = "Въведете валиден имейл адрес.")]
        [Display(Name = "Имейл")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Име")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Въведете валиден телефон.")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Имейл потвърден")]
        public bool EmailConfirmed { get; set; }
    }
}
