using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace DATN_GO.Models
{
    public enum StoreStatus
    {
        [Display(Name = "Chờ duyệt")]
        PendingApproval,      // Chờ duyệt
        [Display(Name = "Chưa duyệt")]
        NotApproved,         // Chưa duyệt
        [Display(Name = "Từ chối")]
        Rejected,            // Từ chối
        [Display(Name = "Hoạt động")]
        Active,              // Hoạt động
        [Display(Name = "Ngừng hoạt động")]
        Inactive             // Ngừng hoạt động
    }

    public class Stores
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength(50)]
        [MinLength(2, ErrorMessage = "Tên phải từ 2 đến 50 kí tự.")]
        public string Name { get; set; }
        [MaxLength(50)]
        public string? RepresentativeName { get; set; }

        [MaxLength]
        public string? Address { get; set; }

        public float Longitude { get; set; }
        public float Latitude { get; set; }

        [MaxLength]
        public string? Avatar { get; set; }

        public StoreStatus Status { get; set; }

        public string? Slug { get; set; }

        [MaxLength]
        public string? CoverPhoto { get; set; }

        [MaxLength(50)]
        public string? BankAccount { get; set; }

        [MaxLength(50)]
        public string? Bank { get; set; }

        [MaxLength(50)]
        public string? BankAccountOwner { get; set; }

        public float Rating { get; set; }

        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }

        [JsonIgnore]
        public virtual Users? User { get; set; }
        [JsonIgnore]

        public ICollection<Products>? Products { get; set; }
        [JsonIgnore]
        public ICollection<ShippingMethods>? ShippingMethods { get; set; }
    }
}
