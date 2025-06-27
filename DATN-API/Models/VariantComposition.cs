using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class VariantComposition
    {
        [Key]
        public int Id { get; set; }

        public int? ProductVariantId { get; set; }
        public int? VariantValueId { get; set; }
        public int? VariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        public virtual Products? ProductVariant { get; set; }
        [ForeignKey("VariantValueId")]
        public virtual VariantValues? VariantValue { get; set; }
        [ForeignKey("VariantId")]
        public virtual Variants? Variant { get; set; }
    }
}
