using DATN_API.Models;

namespace DATN_API.ViewModels
{
    public class ProductFullCreateViewModel
    {
        public Products Product { get; set; }
        public decimal? Price { get; set; } // Dùng nếu không có biến thể
        public List<VariantCreateModel>? Variants { get; set; }
        public List<VariantCombinationModel>? Combinations { get; set; }
    }
    public class ProductCreateViewModel
    {
        public string Name { get; set; }
        public string? Brand { get; set; }
        public string? PlaceOfOrigin { get; set; }
        public int CategoryId { get; set; }
        public int? Quantity { get; set; } // optional for variants
        public decimal? CostPrice { get; set; } // optional for variants
        public decimal? Price { get; set; } // optional for variants
        public int? Weight { get; set; }
        public float? Height { get; set; }
        public float? Width { get; set; }
        public float? Length { get; set; }
        public IFormFile? Image { get; set; }

        public List<VariantCreateModel>? Variants { get; set; }
        public List<VariantCombinationModel>? Combinations { get; set; }
    }

    public class VariantCreateModel
    {
        public string Name { get; set; }
        public List<string> Values { get; set; } = new();
    }

    public class VariantCombinationModel
    {
        public List<string> Values { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int Quantity { get; set; }
        public int Weight { get; set; }
        public float? Width { get; set; }
        public float? Height { get; set; }
        public float? Length { get; set; }
        public string? ImageUrl { get; set; }
    }

}
