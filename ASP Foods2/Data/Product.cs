using Humanizer;

namespace ASP_Foods2.Data
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CatalogId { get; set; }
        public int CategoryId { get; set; } 
        public Category Categories { get; set; }//таблицата
        public int TypeProductId { get; set; }
        public TypeProduct TypeProducts { get; set; }//таблицата
        public int BrandId { get; set; }
        public Brand Brands { get; set; }//таблицата
        public int UnitId { get; set; }
        public Unit Units { get; set; }//таблицата
        public decimal Quantity { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageLink { get; set; }
        public DateTime DateAdded { get; set; }
        public ICollection<Cart> Carts { get; set; }


    }
}
