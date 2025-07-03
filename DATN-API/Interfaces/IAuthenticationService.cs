using System.Threading.Tasks;

namespace DATN_API.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> IsEmailExistAsync(string email);
        Task<bool> IsPhoneExistAsync(string phone);
        Task<string> SendOtpToEmailAsync(string email);
        Task<string> SendOtpToPhoneAsync(string phone);
        Task<bool> VerifyOtpAsync(string identifier, string code);
        Task<string> ChangeEmailAsync(int userId, string newEmail, string otpCode);
        // ...bổ sung các hàm khác nếu cần
    }
}
