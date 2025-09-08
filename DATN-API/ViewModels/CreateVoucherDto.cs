namespace DATN_API.ViewModels
{
    // ViewModels/CreateVoucherDto.cs
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
        public bool ApplyAllCategories { get; set; } // NEW
        public bool ApplyAllProducts { get; set; }   // NEW
        public int? CategoryId { get; set; }
        public List<int>? SelectedProductIds { get; set; } // NEW

        // shop/sàn + audit
        public int? StoreId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? CreatedByRoleId { get; set; }
    }

    // ViewModels/UpdateVoucherDto.cs
    public class UpdateVoucherDto : CreateVoucherDto
    {
        public int Id { get; set; }
    }


    public class ApplyVoucherRequestDto
    {
        public int VoucherId { get; set; }
        public int UserId { get; set; } // NEW – để check đã dùng
        public decimal OrderSubtotal { get; set; }
        public IEnumerable<int> ProductIdsInCart { get; set; } = Enumerable.Empty<int>();
        public int? CategoryIdInCart { get; set; }
    }
    public class ApplyVoucherResponseDto
    {
        public decimal DiscountOnSubtotal { get; set; }
        public decimal DiscountOnShipping { get; set; }
        public string Reason { get; set; } = "";
    }

}