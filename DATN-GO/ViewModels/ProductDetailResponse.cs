namespace DATN_GO.ViewModels
{
    public class ProductDetailResponse
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int StoreId { get; set; }
        public string Name { get; set; }
        public string? Brand { get; set; }
        public int? Weight { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? MainImage { get; set; }
        public string Status { get; set; }
        public int Quantity { get; set; }
        public int Views { get; set; }
        public float? Rating { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public decimal? CostPrice { get; set; }
        public float? Height { get; set; }
        public float? Width { get; set; }
        public float? Length { get; set; }
        public string? PlaceOfOrigin { get; set; }
        public string? Hashtag { get; set; }
        public string? CategoryName { get; set; }    // ✅ thêm
        public string? StoreName { get; set; }
        public List<string> Images { get; set; } = new();

        public List<VariantGroupDto> VariantGroups { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
    }

    public class VariantGroupDto
    {
        public int VariantId { get; set; }
        public string VariantName { get; set; }
        public string? Type { get; set; }
        public List<VariantValueDto> Values { get; set; } = new();
    }

    public class VariantValueDto
    {
        public int Id { get; set; }
        public string ValueName { get; set; }
        public string? Type { get; set; }
        public string? Image { get; set; }
        public string? ColorHex { get; set; }
    }

    public class ProductVariantDto
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int Quantity { get; set; }
        public int Weight { get; set; }
        public float? Height { get; set; }
        public float? Width { get; set; }
        public float? Length { get; set; }
        public string? Image { get; set; }


        public List<string> Images { get; set; } = new(); // ảnh cấp variant (nếu có)
        public List<int> VariantValueIds { get; set; } = new();
        public List<string> VariantValueNames { get; set; } = new();
    }

}