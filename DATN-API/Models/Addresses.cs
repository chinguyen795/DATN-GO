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

        [MaxLength(13)]
        [RegularExpression(@"^(0|\+84)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-5]|9[0-9])[0-9]{7}$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        public required string Phone { get; set; }


        public DateTime CreateAt { get; set; }

        public DateTime? UpdateAt { get; set; }

        [MinLength(5, ErrorMessage = "Mô tả quá ngắn, phải từ 5 kí tự.")]
        [MaxLength]
        public string? Description { get; set; }

        public AddressStatus Status { get; set; }

        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]
        public virtual Cities? City { get; set; }
        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }
    }
}
