using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class Prices
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }

        [ForeignKey("VariantComposition")]
        public int VariantCompositionId { get; set; }
        [JsonIgnore]
        public virtual VariantComposition? VariantComposition { get; set; }

        public decimal Price { get; set; }

        [JsonIgnore]
        public ICollection<OrderDetails>? OrderDetails { get; set; }
    }
}
