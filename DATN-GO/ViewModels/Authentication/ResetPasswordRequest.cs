using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels.Authentication
{
    public class ResetPasswordRequest
    {
        // Kiểm tra xem Identifier (email/phone) có bị bỏ trống không
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại.")]
        [RegularExpression(@"^((\+84|84)(3|5|7|8|9)[0-9]{8}|[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$",
            ErrorMessage = "Vui lòng nhập đúng email hoặc số điện thoại (+84).")]
        public string Identifier { get; set; }

        // Kiểm tra mật khẩu có phải là chuỗi không rỗng và phải đủ độ dài
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(50, MinimumLength = 7, ErrorMessage = "Mật khẩu phải từ 7 ký tự.")]
        [MaxLength(50, ErrorMessage = "Mật khẩu không được vượt quá 50 ký tự.")]
        public string Password { get; set; }

        // Kiểm tra xác nhận mật khẩu có khớp với mật khẩu đã nhập
        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}