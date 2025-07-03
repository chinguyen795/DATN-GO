using DATN_API.Data;
using DATN_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsPhoneExistAsync(string phone)
        {
            return await _context.Users.AnyAsync(u => u.Phone == phone);
        }

        public async Task<string> SendOtpToEmailAsync(string email)
        {
            // TODO: Gửi OTP qua email, lưu OTP vào cache hoặc DB nếu cần
            return "OTP sent to email";
        }

        public async Task<string> SendOtpToPhoneAsync(string phone)
        {
            // TODO: Gửi OTP qua SMS, lưu OTP vào cache hoặc DB nếu cần
            return "OTP sent to phone";
        }

        public async Task<bool> VerifyOtpAsync(string identifier, string code)
        {
            // TODO: Kiểm tra OTP hợp lệ
            return true;
        }

        public async Task<string> ChangeEmailAsync(int userId, string newEmail, string otpCode)
        {
            // TODO: Đổi email sau khi xác thực OTP
            return "Email changed";
        }
    }
}
