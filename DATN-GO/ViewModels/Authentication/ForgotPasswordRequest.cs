using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels.Authentication
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Tài khoản không thể để trống.")]
        [Display(Name = "Tài khoản")]
        [StringLength(255, ErrorMessage = "Tài khoản không được quá 255 ký tự.")]
        public string Identifier { get; set; }
    }
}