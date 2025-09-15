using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DATN_API.Models
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
        [Key] public int Id { get; set; }

        public decimal Reduce { get; set; }
        public VoucherType Type { get; set; }
        public decimal MinOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public VoucherStatus Status { get; set; }

        [JsonIgnore] public ICollection<ProductVouchers> ProductVouchers { get; set; } = new List<ProductVouchers>();

        [JsonIgnore] public ICollection<Orders>? Orders { get; set; }

        // ==== PHẠM VI ÁP DỤNG ====
        [ForeignKey("Category")] public int? CategoryId { get; set; }
        [JsonIgnore]
        public virtual ICollection<Categories> Categories { get; set; } = new List<Categories>();


        public bool ApplyAllCategories { get; set; } = false; // NEW
        public bool ApplyAllProducts { get; set; } = false; // NEW

        // ==== SHOP / SÀN ====
        [ForeignKey("Store")] public int? StoreId { get; set; }
        [JsonIgnore] public virtual Stores? Store { get; set; }

        [Range(1, int.MaxValue)] public int Quantity { get; set; }
        public bool IsPercentage { get; set; } = false;
        [Column(TypeName = "decimal(18,2)")] public decimal? MaxDiscount { get; set; }
        public int UsedCount { get; set; } = 0;

        public int? CreatedByUserId { get; set; }
        public int? CreatedByRoleId { get; set; } // 3=admin, 2=shop

        // Chống race khi trừ số lượng
        [Timestamp] public byte[]? RowVersion { get; set; } // NEW
    }
}