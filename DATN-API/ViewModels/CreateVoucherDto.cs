// ViewModels/CreateVoucherDto.cs
using System.ComponentModel.DataAnnotations;
using DATN_API.Models;

namespace DATN_API.ViewModels
{
    public class CreateVoucherDto
    {
        public bool IsPercentage { get; set; }
        public decimal Reduce { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal MinOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Quantity { get; set; }

        // phạm vi
        public bool ApplyAllCategories { get; set; }
        public bool ApplyAllProducts { get; set; }

        // ⬇⬇⬇ CHÍNH: nhiều category + nhiều product
        public List<int>? CategoryIds { get; set; }          // NEW (thay cho CategoryId đơn)
        public List<int>? SelectedProductIds { get; set; }   // Giữ nguyên ý nhưng cho phép nhiều id

        // shop/sàn + audit
        public int? StoreId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? CreatedByRoleId { get; set; }
    }

    public class UpdateVoucherDto : CreateVoucherDto
    {
        public int Id { get; set; }
    }

    public class ApplyVoucherRequestDto
    {
        public int VoucherId { get; set; }
        public int UserId { get; set; }
        public decimal OrderSubtotal { get; set; }

        // Cart có thể chứa nhiều product (=> thuộc nhiều category)
        public IEnumerable<int> ProductIdsInCart { get; set; } = Enumerable.Empty<int>();

        // ⬇⬇⬇ CHÍNH: nhiều category trong giỏ (suy ra từ product hoặc client gửi)
        public IEnumerable<int>? CategoryIdsInCart { get; set; }
    }

    public class ApplyVoucherResponseDto
    {
        public decimal DiscountOnSubtotal { get; set; }
        public decimal DiscountOnShipping { get; set; }
        public string Reason { get; set; } = "";
    }
}