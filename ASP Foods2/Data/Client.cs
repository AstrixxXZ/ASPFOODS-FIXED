using Microsoft.AspNetCore.Identity;

namespace ASP_Foods2.Data
{
    public class Client : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ICollection<Order> Orders { get; set; }

    }
}
