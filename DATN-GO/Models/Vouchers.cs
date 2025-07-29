using DATN_GO.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public enum VoucherStatus
    {
        [Display(Name = "Còn hạn")]
        Valid,
        [Display(Name = "Hết hạn")]
        Expired,
        [Display(Name = "Đã sử dụng")]
        Used,
        [Display(Name = "Đã lưu")]
        Saved,
    }

    public enum VoucherType
    {
        [Display(Name = "Mã của sàn")]
        Platform,
        [Display(Name = "Mã của shop")]
        Shop
    }

    public class Vouchers
    {
        [Key]
        public int Id { get; set; }

        public decimal Reduce { get; set; }

        public VoucherType Type { get; set; }

        public decimal MinOrder { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public VoucherStatus Status { get; set; }
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

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải từ 1 trở lên")]
        public int Quantity { get; set; }
    }
}