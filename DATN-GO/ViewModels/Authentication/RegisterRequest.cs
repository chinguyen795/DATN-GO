using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels.Authentication
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại.")]
        [RegularExpression(@"^((\+84|84)(3|5|7|8|9)[0-9]{8}|[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$",
            ErrorMessage = "Vui lòng nhập đúng email hoặc số điện thoại (+84).")]
        public string Identifier { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "Mật khẩu phải từ 7-15 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]).{7,15}$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}