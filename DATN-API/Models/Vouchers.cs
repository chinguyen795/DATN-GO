using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class Vouchers
    {
        [Key]
        public int Id { get; set; }

        public decimal Reduce { get; set; }

        [MaxLength(50)]
        public string Type { get; set; }

        public decimal MinOrder { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }
        [JsonIgnore]
        public ICollection<ProductVouchers>? ProductVouchers { get; set; }
        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }

        [ForeignKey("Category")]
        public int? CategoryId { get; set; }
        [JsonIgnore]
        public virtual Categories? Category { get; set; }

        [ForeignKey("Store")]
        public int? StoreId { get; set; }
        [JsonIgnore]
        public virtual Stores? Store { get; set; }

        public int Quantity { get; set; }
    }
}
