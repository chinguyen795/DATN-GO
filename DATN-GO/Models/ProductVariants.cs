using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class ProductVariants
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }

        public decimal Price { get; set; }
        public int Weight { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal CostPrice { get; set; }
        public string? Image { get; set; }
        public int Quantity { get; set; }
        public float? Height { get; set; }

        public float? Width { get; set; }

        public float? Length { get; set; }

        [JsonIgnore]
        public ICollection<ProductImages>? ProductImages { get; set; }
        [JsonIgnore]
        public ICollection<VariantComposition>? VariantCompositions { get; set; }
    }
}
