using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DATN_API.Models
{
    public enum AddressStatus
    {
        [Display(Name = "Mặc định")]
        Default,
        [Display(Name = "Không mặc định")]
        NotDefault
    }

    public class Addresses
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        [MaxLength(50)]
        [MinLength(2, ErrorMessage = "Tên phải từ 2 đến 50 kí tự.")]
        public required string Name { get; set; }

        // ✅ Validate chuẩn VN: 10–11 số, đầu số hợp lệ (03,05,07,08,09 hoặc +84...)
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(
            @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-689]|9\d)\d{7}$",
            ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        public required string Phone { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        [MinLength(5, ErrorMessage = "Mô tả quá ngắn, phải từ 5 kí tự.")]
        [MaxLength(255, ErrorMessage = "Mô tả quá dài, tối đa 255 kí tự.")]
        public string? Description { get; set; }

        public AddressStatus Status { get; set; }

        [ForeignKey("District")]
        public int? DistrictId { get; set; }

        [ForeignKey("Ward")]
        public int? WardId { get; set; }

        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]
        public virtual Cities? City { get; set; }
        [JsonIgnore]
        public virtual Districts? District { get; set; }
        [JsonIgnore]
        public virtual Wards? Ward { get; set; }

        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }
    }
}