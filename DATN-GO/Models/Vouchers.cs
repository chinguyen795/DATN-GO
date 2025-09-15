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
        public int Id { get; set; }
        public bool IsPercentage { get; set; }
        public decimal Reduce { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal MinOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Quantity { get; set; }
        public VoucherStatus Status { get; set; }

        public int? CategoryId { get; set; }
        public List<int>? CategoryIds { get; set; } = new();
        public int? StoreId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int CreatedByRoleId { get; set; }
        public VoucherType Type { get; set; }

        public int? UsedCount { get; set; } = 0;
        // ⚡️ thêm các field mới cho Admin
        public bool ApplyAllCategories { get; set; } = false;
        public bool ApplyAllProducts { get; set; } = false;
        public List<int>? SelectedProductIds { get; set; } = new();
    }

}