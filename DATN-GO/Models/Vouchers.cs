using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public enum VoucherStatus
    {
        [Display(Name = "Còn hạn")] Valid,
        [Display(Name = "Hết hạn")] Expired,
        [Display(Name = "Đã sử dụng")] Used,
        [Display(Name = "Đã lưu")] Saved,
    }

    public enum VoucherType
    {
        [Display(Name = "Mã của sàn")] Platform,
        [Display(Name = "Mã của shop")] Shop
    }

    public class Vouchers
    {
        [Key]
        public int Id { get; set; }

        // Giảm theo % nếu IsPercentage = true; hoặc số tiền nếu false
        public decimal Reduce { get; set; }
        public VoucherType Type { get; set; }
        public decimal MinOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public VoucherStatus Status { get; set; }
        [JsonIgnore] public ICollection<ProductVouchers>? ProductVouchers { get; set; }
        [JsonIgnore] public ICollection<Orders>? Orders { get; set; }

        [ForeignKey("Category")]
        public int? CategoryId { get; set; }
        [JsonIgnore] public virtual Categories? Category { get; set; }

        [ForeignKey("Store")]
        public int? StoreId { get; set; } // null = voucher admin/sàn
        [JsonIgnore] public virtual Stores? Store { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải từ 1 trở lên")]
        public int Quantity { get; set; }

        /// <summary>Interpret Reduce là % (true) hay số tiền (false).</summary>
        public bool IsPercentage { get; set; } = false;

        /// <summary>Nếu là % có thể giới hạn mức giảm tối đa.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscount { get; set; }

        /// <summary>Đã dùng bao nhiêu lần.</summary>
        public int? UsedCount { get; set; } = 0;

       

        /// <summary>Thông tin người tạo để phân quyền (admin=3, shop=2).</summary>
        public int? CreatedByUserId { get; set; }
        public int? CreatedByRoleId { get; set; } // 3 = admin, 2 = shop
    }
}
