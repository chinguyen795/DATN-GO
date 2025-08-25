using DATN_GO.Models;
using System.Collections.Generic;

namespace DATN_GO.ViewModels
{
    public class HomeViewModel
    {
        public List<StoreHomeViewModel> Stores { get; set; } = new();
        public List<ProductHomeViewModel> FeaturedProducts { get; set; } = new();
        public List<ProductHomeViewModel> SuggestedProducts { get; set; } = new();
        public List<CategoryHomeViewModel> TrendCategories { get; set; }
        public List<CategoryHomeViewModel> Categories { get; set; } = new();
        public List<Categories> Categoriess { get; set; } = new();
        public List<SlideViewModel> Slides { get; set; } = new();


    }
    public class SlideViewModel
    {
        public string? Image { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
    public class CategoryHomeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int Rank { get; set; }
        public int TotalProducts { get; set; }
        public CategoryStatus Status { get; set; }
    }
    public class StoreHomeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public float Rating { get; set; }
        public string? Status { get; set; }
    }
    public class ProductHomeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public string? CategoryName { get; set; }
        public string? StoreName { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public double Rating { get; set; }
    }
    public class SearchViewModel
    {
        public string Query { get; set; }
        public List<ProductHomeViewModel> Products { get; set; } = new();
        public List<StoreHomeViewModel> Stores { get; set; } = new();
    }
}