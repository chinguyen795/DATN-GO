using System.ComponentModel.DataAnnotations;
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

    }
}
