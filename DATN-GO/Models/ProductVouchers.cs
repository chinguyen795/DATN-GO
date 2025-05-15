using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class ProductVouchers
    {
        public int ProductId { get; set; }
        public int VoucherId { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }
        [JsonIgnore]
        public virtual Vouchers? Voucher { get; set; }
    }
}
