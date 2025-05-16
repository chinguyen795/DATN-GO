using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class ProductSkus
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [MaxLength(50)]
        public string Sku { get; set; }

        public decimal Price { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }
        [JsonIgnore]
        public ICollection<SkusValues>? SkusValues { get; set; }
    }
}
