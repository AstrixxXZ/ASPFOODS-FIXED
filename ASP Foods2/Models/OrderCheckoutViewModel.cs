namespace ASP_Foods2.Models
{
    public class OrderCheckoutViewModel
    {
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public List<OrderCheckoutItemViewModel> Items { get; set; } = [];
        public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
        public int TotalQuantity => Items.Sum(item => item.Quantity);
        public bool HasItems => Items.Count > 0;
    }

    public class OrderCheckoutItemViewModel
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CatalogId { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
