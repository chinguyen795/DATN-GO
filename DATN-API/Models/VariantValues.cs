using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class VariantValues
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Variant")]
        public int VariantId { get; set; }

        [MaxLength(50)]
        public string ValueName { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }

        [MaxLength(50)]
        public string? Image { get; set; }

        [MaxLength(7)]
        public string? colorHex { get; set; }

        [JsonIgnore]
        public virtual Variants? Variant { get; set; }
    }
}
