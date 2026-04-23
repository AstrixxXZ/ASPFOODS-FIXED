namespace ASP_Foods2.Data
{
    public class Cart
    {
        public int Id { get; set; }
        public string ClientId { get; set; }
        public Client Clients { get; set; }
        public int ProductId { get; set; }
        public Product Products { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }

    }
}
