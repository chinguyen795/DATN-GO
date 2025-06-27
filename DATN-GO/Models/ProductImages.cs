using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATN_GO.Models
{
    public class ProductImages
    {
        [Key]
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public int ProductId { get; set; }

        public string? Media { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual Variants? ProductVariant { get; set; }
        [ForeignKey("ProductId")]
        public virtual Products? Product { get; set; }
    }
}
