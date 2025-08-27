using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.Json.Serialization;
using Twilio.TwiML.Messaging;
using Twilio.TwiML.Voice;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DATN_API.Models
{
    public enum UserStatus
    {
        [Display(Name = "Hoạt động")]
        Active,
        [Display(Name = "Ngừng hoạt động")]
        Inactive
    }

    public enum GenderType
    {
        [Display(Name = "Nam")]
        Male,
        [Display(Name = "Nữ")]
        Female,
        [Display(Name = "Khác")]
        Other
    }

    public class Users
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; }
        [Required]
        [MinLength(7, ErrorMessage = "Mật khẩu phải từ 7 ký tự trở lên.")]
        [MaxLength]
        public string Password { get; set; }
        [Required]
        [MinLength(2, ErrorMessage = "Tên phải từ 2 ký tự trở lên.")]
        [MaxLength(50)]
        public string FullName { get; set; }


        // using System.ComponentModel.DataAnnotations;

        [MaxLength(13)]
        [RegularExpression(
            @"^(?:0|\+84)(?:3[2-9]|5[689]|7[06789]|8[1-5]|9\d)\d{7}$",
            ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam."
        )]
        public required string Phone { get; set; }

        [MaxLength]
        public string? Avatar { get; set; }
        public UserStatus Status { get; set; }
        public GenderType Gender { get; set; }
        [MaxLength(12)]
        public string? CitizenIdentityCard { get; set; }
        public decimal? Balance { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public DateTime? BirthDay { get; set; }
        [JsonIgnore]
        public virtual Roles? Role { get; set; }
        [JsonIgnore]
        public ICollection<Addresses>? Address { get; set; }
        [JsonIgnore]
        public ICollection<Messages>? SentMessages { get; set; }
        [JsonIgnore]
        public ICollection<Messages>? ReceivedMessages { get; set; }
        [JsonIgnore]
        public ICollection<Posts>? Posts { get; set; }
        [JsonIgnore]
        public ICollection<Decorates>? Decorates { get; set; }
        [JsonIgnore]
        public virtual Stores? Store { get; set; }
        [JsonIgnore]
        public ICollection<Carts>? Carts { get; set; }
        [JsonIgnore]
        public ICollection<Orders>? Orders { get; set; }
        [JsonIgnore]
        public ICollection<Reviews>? Reviews { get; set; }
        [JsonIgnore]
        public ICollection<UserTradingPayment>? UserTradingPayments { get; set; }
    }

    public class GoogleLoginDto
    {
        public string IdToken { get; set; }
    }
}