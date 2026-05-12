using System;
using System.ComponentModel.DataAnnotations;

namespace ASP_Foods2.Data
{
    public class PromoCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string Code { get; set; } = string.Empty;

        [StringLength(160)]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "100")]
        public decimal DiscountPercent { get; set; }

        [Range(typeof(decimal), "0", "999999")]
        public decimal MinimumOrderAmount { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
