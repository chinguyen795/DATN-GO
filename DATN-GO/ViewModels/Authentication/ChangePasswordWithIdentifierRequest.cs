using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels.Authentication
{
    public class ChangePasswordWithIdentifierRequest
    {
        [Required(ErrorMessage = "Không tìm thấy tài khoản. Vui lòng đăng nhập lại.")]
        public string Identifier { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [MinLength(7, ErrorMessage = "Mật khẩu hiện tại phải từ 7 ký tự trở lên.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(7, ErrorMessage = "Mật khẩu mới phải từ 7 ký tự trở lên.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmNewPassword { get; set; }
    }
}