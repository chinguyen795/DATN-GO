using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DATN_GO.Service
{
	public class AuthenticationService
	{
		private readonly HttpClient _httpClient;
		private readonly string _baseUrl;

		// Loại bỏ các biến quản lý trạng thái OTP cục bộ vì API đã xử lý
		// private static readonly ConcurrentDictionary<string, DateTime> _otpSentTimes = new();
		// private readonly TimeSpan _otpCooldownTime = TimeSpan.FromMinutes(2);

		public AuthenticationService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_baseUrl = configuration["ApiSettings:BaseUrl"];
		}

		
		public async Task<(bool Success, string Message)> SendVerificationCodeAsync(string identifier)
		{
			// API của bạn nhận một string input trực tiếp từ [FromBody]
			// Do đó, cần serialize identifier thành một chuỗi JSON hợp lệ.
			var content = new StringContent(JsonSerializer.Serialize(identifier), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/SendVerificationCode", content);

			if (response.IsSuccessStatusCode)
			{
				return (true, await response.Content.ReadAsStringAsync());
			}
			else
			{
				var errorMessage = await response.Content.ReadAsStringAsync();
				return (false, errorMessage);
			}
		}

		// Điều chỉnh phương thức VerifyCodeAsync
		public async Task<(bool Success, string Message)> VerifyCodeAsync(string identifier, string code)
		{
			// API của bạn nhận một đối tượng VerifyRequest
			var payload = new VerifyRequest
			{
				Identifier = identifier,
				Code = code
			};

			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/VerifyCode", content);

			if (response.IsSuccessStatusCode)
			{
				// API trả về `true` hoặc `false` dưới dạng JSON boolean
				var result = JsonSerializer.Deserialize<bool>(await response.Content.ReadAsStringAsync());
				return (result, result ? "Xác thực mã thành công!" : "Mã xác thực không đúng hoặc đã hết hạn!");
			}
			else
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				return (false, errorContent);
			}
		}

		// Điều chỉnh phương thức RegisterAsync
		public async Task<(bool Success, string Message)> RegisterAsync(string identifier, string password, string confirmPassword)
		{
			// API của bạn nhận một đối tượng RegisterRequest
			var payload = new RegisterRequest
			{
				Identifier = identifier,
				Password = password,
				ConfirmPassword = confirmPassword
			};
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/Register", content);

			if (response.IsSuccessStatusCode)
			{
				return (true, await response.Content.ReadAsStringAsync());
			}
			else
			{
				var errorMessage = await response.Content.ReadAsStringAsync();
				return (false, errorMessage);
			}
		}

		public async Task<LoginResult?> LoginAsync(string identifier, string password)
		{
			var payload = new LoginRequest
			{
				Identifier = identifier,
				Password = password
			};
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/Login", content);

			if (!response.IsSuccessStatusCode)
				return null;

			var json = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<LoginResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

        public async Task<(bool Success, string Message)> ChangePasswordAsync(string identifier, string currentPassword, string newPassword, string confirmNewPassword)
        {
            var payload = new ChangePasswordWithIdentifierRequest
            {
                Identifier = identifier,
                CurrentPassword = currentPassword,
                NewPassword = newPassword,
                ConfirmNewPassword = confirmNewPassword
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/ChangePasswordWithIdentifier", content);

            if (response.IsSuccessStatusCode)
            {
                return (true, await response.Content.ReadAsStringAsync());
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return (false, errorMessage);
            }
        }

        public async Task<(bool Success, string Message)> SendOtpToNewEmailAsync(string newEmail)
        {
            var content = new StringContent(JsonSerializer.Serialize(newEmail), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/SendOtpToNewEmail", content);

            if (response.IsSuccessStatusCode)
            {
                return (true, await response.Content.ReadAsStringAsync());
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return (false, errorMessage);
            }
        }

        public async Task<(bool Success, string Message)> ChangeEmailAsync(int userId, string newEmail, string otpCode)
        {
            var payload = new ChangeEmailRequest
            {
                UserId = userId, 
                NewEmail = newEmail,
                OtpCode = otpCode
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/ChangeEmail", content);

            if (response.IsSuccessStatusCode)
            {
                return (true, await response.Content.ReadAsStringAsync());
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return (false, errorMessage);
            }
        }

        public async Task<bool> IsEmailExistAsync(string email)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Authentication/IsEmailExist?email={Uri.EscapeDataString(email)}");
            if (!response.IsSuccessStatusCode)
                return false;
            var json = await response.Content.ReadAsStringAsync();
            return bool.TryParse(json, out var exists) && exists;
        }


        public class VerifyRequest
		{
			public string Identifier { get; set; }
			public string Code { get; set; }
		}

		public class RegisterRequest
		{
			public string Identifier { get; set; }
			public string Password { get; set; }
			public string ConfirmPassword { get; set; }
		}

		public class LoginRequest
		{
			public string Identifier { get; set; }
			public string Password { get; set; }
		}

		public class LoginResult
		{
			public int Id { get; set; }
			public string Email { get; set; }
			public string PhoneNumber { get; set; }
			public string FullName { get; set; }
			public int Roles { get; set; }
			public string Token { get; set; }
		}

        public class ChangePasswordWithIdentifierRequest
        {
            public string Identifier { get; set; }
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmNewPassword { get; set; }
        }

        public class ChangeEmailRequest
        {
            public int UserId { get; set; }
            public string NewEmail { get; set; }
            public string OtpCode { get; set; }
        }
    }
}