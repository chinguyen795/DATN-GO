using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class ProductVouchers
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }

        [ForeignKey("Voucher")]
        public int VoucherId { get; set; }
        [JsonIgnore]
        public virtual Vouchers? Voucher { get; set; }

        public decimal PriceReduce { get; set; }
    }
}
