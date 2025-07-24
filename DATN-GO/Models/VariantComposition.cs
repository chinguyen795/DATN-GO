using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DATN_GO.Models;

namespace DATN_GO.Models
{
    public class VariantComposition
    {
        [Key]
        public int Id { get; set; }

        public int? ProductId { get; set; }                // Thêm dòng này
        public int? ProductVariantId { get; set; }
        public int? VariantValueId { get; set; }
        public int? VariantId { get; set; }

        [ForeignKey("ProductId")]
        [JsonIgnore]
        public virtual Products? Product { get; set; }     // Thêm dòng này

        [ForeignKey("ProductVariantId")]
        [JsonIgnore]
        public virtual ProductVariants? ProductVariant { get; set; }

        [ForeignKey("VariantValueId")]
        [JsonIgnore]
        public virtual VariantValues? VariantValue { get; set; }

        [ForeignKey("VariantId")]
        [JsonIgnore]
        public virtual Variants? Variant { get; set; }
    }
}