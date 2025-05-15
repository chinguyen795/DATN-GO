using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Options
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [MaxLength(50)]
        public string OptionName { get; set; }

        public virtual Products Product { get; set; }
        [JsonIgnore]
        public ICollection<OptionValues>? OptionValues { get; set; }
        [JsonIgnore]
        public ICollection<SkusValues>? SkusValues { get; set; }
    }
}
