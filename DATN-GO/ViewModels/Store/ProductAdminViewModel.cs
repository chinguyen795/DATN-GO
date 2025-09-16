namespace DATN_GO.ViewModels.Store
{
    public class ProductAdminViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? MainImage { get; set; }
        public string? Description { get; set; }
        public decimal OriginalPrice => CostPrice.HasValue
    ? Math.Round(CostPrice.Value * 1.3m)
    : 0;
        public string? Brand { get; set; }
        public decimal? Weight { get; set; }
        public string? Slug { get; set; }
        public string? Status { get; set; }
        public int Quantity { get; set; }
        public int Views { get; set; }
        public float? Rating { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public decimal? CostPrice { get; set; }
        public string? PackageSize { get; set; }
        public string? PlaceOfOrigin { get; set; }
        public string? Hashtag { get; set; }

        public string? CategoryName { get; set; }
        public string? StoreName { get; set; }
    }
}