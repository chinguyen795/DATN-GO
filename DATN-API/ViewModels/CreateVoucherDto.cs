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

        // đã bỏ IsFreeShipping, ApplyAllProducts
        public int? CategoryId { get; set; }  // bắt buộc có CategoryId hoặc ProductVouchers được map khi tạo
        public int? StoreId { get; set; }
        public int CreatedByUserId { get; set; }
        public int CreatedByRoleId { get; set; }
    }

    public class UpdateVoucherDto : CreateVoucherDto
    {
        public int Id { get; set; }
    }

    public class ApplyVoucherRequestDto
    {
        public int VoucherId { get; set; }
        public decimal OrderSubtotal { get; set; }
        // đã bỏ ShippingFee
        public IEnumerable<int> ProductIdsInCart { get; set; } = Enumerable.Empty<int>();
        public int? CategoryIdInCart { get; set; }
    }

    public class ApplyVoucherResponseDto
    {
        public decimal DiscountOnSubtotal { get; set; }
        public decimal DiscountOnShipping { get; set; } // vẫn giữ field để không phá vỡ client, nhưng luôn = 0
        public string Reason { get; set; } = "OK";
    }
}
