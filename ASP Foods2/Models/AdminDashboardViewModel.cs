using System;
using System.Collections.Generic;

namespace ASP_Foods2.Models
{
    public class AdminDashboardViewModel
    {
        public int ProductCount { get; set; }
        public int BrandCount { get; set; }
        public int OrderCount { get; set; }
        public int CategoryCount { get; set; }
        public int SupportMessageCount { get; set; }
        public int TotalOrderedQuantity { get; set; }
        public List<AdminRecentOrderViewModel> RecentOrders { get; set; } = new();
    }

    public class AdminRecentOrderViewModel
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCatalogId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
