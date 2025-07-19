using System.ComponentModel.DataAnnotations;

namespace DATN_GO.ViewModels.Authentication
{
    public class RegisterIdentifierRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại.")]
        [RegularExpression(@"^((\+84|84)(3|5|7|8|9)[0-9]{8}|[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})$",
  ErrorMessage = "Vui lòng nhập đúng email hoặc số điện thoại (+84).")]
        public string Identifier { get; set; }
    }
}
