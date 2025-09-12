using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels
{
    public class SalesRegistrationViewModel
    {
        public string? Avatar { get; set; }
        public string? CoverPhoto { get; set; }
        public string? CitizenIdentityCard { get; set; }
        public string? RepresentativeName { get; set; }
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản.")]
        public string? BankAccount { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản.")]
        public string? BankAccountOwner { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngân hàng.")]
        public string? Bank { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố.")]
        public string? Province { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quận/huyện.")]
        public string? District { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phường/xã.")]
        public string? Ward { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ lấy hàng.")]
        public string? PickupAddress { get; set; }
        public decimal? MoneyAmout { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ, vui lòng nhập lại.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }


    }
}