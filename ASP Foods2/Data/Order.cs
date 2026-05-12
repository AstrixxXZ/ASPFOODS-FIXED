namespace ASP_Foods2.Data
{
    public class Order
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public Client Clients { get; set; }
        public int ProductId { get; set; }
        public Product Products { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
        public string Status { get; set; } = "Приета";
        public decimal UnitPrice { get; set; }
        public string? PromoCode { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }

    }
}
