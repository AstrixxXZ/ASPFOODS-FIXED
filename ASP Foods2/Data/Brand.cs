namespace ASP_Foods2.Data
{
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
