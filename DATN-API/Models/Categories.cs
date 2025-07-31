using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public enum CategoryStatus
    {
        [Display(Name = "Ẩn")]
        Hidden,
        [Display(Name = "Hiện")]
        Visible
    }

    public class Categories
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        [MinLength(2, ErrorMessage = "Tên phải từ 2 đến 50 kí tự.")]
        public required string Name { get; set; }

        [MaxLength]
        public string? Image { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        public CategoryStatus Status { get; set; }

        [MaxLength(50)]
        [MinLength(2, ErrorMessage = "Hashtag phải từ 2 đến 50 kí tự.")]
        public string? Hashtag { get; set; }

        [MaxLength]
        public string? Description { get; set; }

        [JsonIgnore]
        public ICollection<Products>? Products { get; set; }
        [JsonIgnore]
        public ICollection<Vouchers>? Vouchers { get; set; }
    }
}
