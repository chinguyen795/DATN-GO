namespace DATN_GO.ViewModels
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

        public bool ApplyAllCategories { get; set; }
        public bool ApplyAllProducts { get; set; }
        public List<int>? CategoryIds { get; set; }

        public List<int>? SelectedProductIds { get; set; }

        public int? StoreId { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? CreatedByRoleId { get; set; }
    }

    public class UpdateVoucherDto : CreateVoucherDto
    {
        public int Id { get; set; }
    }
}