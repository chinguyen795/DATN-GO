using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public enum PostStatus
    {
        [Display(Name = "Chưa duyệt")]
        NotApproved,
        [Display(Name = "Đã duyệt")]
        Approved
    }

    public class Posts
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength]
        [MinLength(5, ErrorMessage = "Nội dung phải từ 5 ký tự trở lên.")]
        public string Content { get; set; }
        [MaxLength]
        public string? Image { get; set; }

        public PostStatus Status { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        [JsonIgnore]
        public virtual Users? User { get; set; }
    }
}
