using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class Variants
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [MaxLength(50)]
        public string VariantName { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }

        public virtual Products Product { get; set; }
    }
}
