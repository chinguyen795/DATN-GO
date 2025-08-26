using Microsoft.AspNetCore.Http;

namespace DATN_GO.ViewModels.Decorates
{
    public class DecoratesViewModel
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public int? AdminSettingId { get; set; }

        // Slide info
        public string? TitleSlide1 { get; set; }
        public string? DescriptionSlide1 { get; set; }
        public string? TitleSlide2 { get; set; }
        public string? DescriptionSlide2 { get; set; }
        public string? TitleSlide3 { get; set; }
        public string? DescriptionSlide3 { get; set; }
        public string? TitleSlide4 { get; set; }
        public string? DescriptionSlide4 { get; set; }
        public string? TitleSlide5 { get; set; }
        public string? DescriptionSlide5 { get; set; }

        // Image/video uploads
        public IFormFile? Slide1 { get; set; }
        public IFormFile? Slide2 { get; set; }
        public IFormFile? Slide3 { get; set; }
        public IFormFile? Slide4 { get; set; }
        public IFormFile? Slide5 { get; set; }

        public IFormFile? Image1 { get; set; }
        public IFormFile? Image2 { get; set; }
        public IFormFile? Video { get; set; }

        // Saved file paths (for preview/display)
        public string? Slide1Path { get; set; }
        public string? Slide2Path { get; set; }
        public string? Slide3Path { get; set; }
        public string? Slide4Path { get; set; }
        public string? Slide5Path { get; set; }

        public string? Image1Path { get; set; }
        public string? Image2Path { get; set; }
        public string? VideoPath { get; set; }

        // Decorate image captions
        public string? Title1 { get; set; }
        public string? Title2 { get; set; }
        public string? Description1 { get; set; }
        public string? Description2 { get; set; }
    }
}