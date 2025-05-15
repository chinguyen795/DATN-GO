using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class OptionValues
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Option")]
        public int OptionId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [MaxLength(50)]
        public string ValueName { get; set; }
        [JsonIgnore]
        public virtual Options Option { get; set; }
        [JsonIgnore]
        public virtual Products Product { get; set; }
        [JsonIgnore]
        public ICollection<SkusValues>? SkusValues { get; set; }

    }
}
