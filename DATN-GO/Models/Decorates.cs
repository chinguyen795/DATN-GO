using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class Decorates
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [JsonIgnore]
        public virtual Users? User { get; set; }

        [ForeignKey("AdminSetting")]
        public int? AdminSettingId { get; set; }

        [JsonIgnore]
        public virtual AdminSettings? AdminSetting { get; set; }


        // Video
        [Column(TypeName = "nvarchar(max)")]
        public string? Video { get; set; }


        // Slide 1
        [Column(TypeName = "nvarchar(max)")]
        public string? Slide1 { get; set; }

        [MaxLength(100)]
        public string? TitleSlide1 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DescriptionSlide1 { get; set; }


        // Slide 2
        [Column(TypeName = "nvarchar(max)")]
        public string? Slide2 { get; set; }

        [MaxLength(100)]
        public string? TitleSlide2 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DescriptionSlide2 { get; set; }


        // Slide 3
        [Column(TypeName = "nvarchar(max)")]
        public string? Slide3 { get; set; }

        [MaxLength(100)]
        public string? TitleSlide3 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DescriptionSlide3 { get; set; }


        // Slide 4
        [Column(TypeName = "nvarchar(max)")]
        public string? Slide4 { get; set; }

        [MaxLength(100)]
        public string? TitleSlide4 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DescriptionSlide4 { get; set; }

        // Slide 5
        [Column(TypeName = "nvarchar(max)")]
        public string? Slide5 { get; set; }

        [MaxLength(100)]
        public string? TitleSlide5 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? DescriptionSlide5 { get; set; }


        // Decorate 1


        [Column(TypeName = "nvarchar(max)")]
        public string? Image1 { get; set; }

        [MaxLength(50)]
        public string? Title1 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Description1 { get; set; }



        // Decorate 2
        [Column(TypeName = "nvarchar(max)")]
        public string? Image2 { get; set; }

        [MaxLength(50)]
        public string? Title2 { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Description2 { get; set; }
    }


}
