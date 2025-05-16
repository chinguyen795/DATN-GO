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

		// Loại bỏ phương thức CanSendOtp vì API đã quản lý việc này
		// private bool CanSendOtp(string identifier)
		// {
		//     if (_otpSentTimes.ContainsKey(identifier))
		//     {
		//         var lastSentTime = _otpSentTimes[identifier];
		//         if (DateTime.Now - lastSentTime < _otpCooldownTime)
		//         {
		//             return false;
		//         }
		//     }
		//     return true;
		// }

		// Điều chỉnh phương thức SendVerificationCodeAsync
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
	}
}