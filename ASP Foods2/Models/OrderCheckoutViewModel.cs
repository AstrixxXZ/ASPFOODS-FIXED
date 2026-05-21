namespace ASP_Foods2.Models
{
    public class OrderCheckoutViewModel
    {
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public List<OrderCheckoutItemViewModel> Items { get; set; } = [];
        public string PromoCodeInput { get; set; } = string.Empty;
        public string AppliedPromoCode { get; set; } = string.Empty;
        public string PromoDescription { get; set; } = string.Empty;
        public string PromoMessage { get; set; } = string.Empty;
        public bool PromoMessageIsError { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal SubtotalAmount => Items.Sum(item => item.TotalPrice);
        public decimal TotalAmount => Math.Max(SubtotalAmount - DiscountAmount, 0m);
        public int TotalQuantity => Items.Sum(item => item.Quantity);
        public bool HasItems => Items.Count > 0;
        public bool HasPromoApplied => !string.IsNullOrWhiteSpace(AppliedPromoCode) && DiscountAmount > 0;
    }

    public class OrderCheckoutItemViewModel
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CatalogId { get; set; } = string.Empty;
        public string? ImageLink { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
