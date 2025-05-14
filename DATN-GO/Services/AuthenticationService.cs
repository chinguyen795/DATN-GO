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

		// Lưu trữ thời gian gửi mã OTP cuối cùng
		private static readonly ConcurrentDictionary<string, DateTime> _otpSentTimes = new();

		// Thời gian chờ giữa các lần gửi mã OTP (1 phút)
		private readonly TimeSpan _otpCooldownTime = TimeSpan.FromMinutes(2);

		public AuthenticationService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_baseUrl = configuration["ApiSettings:BaseUrl"];
		}

		// Kiểm tra xem có thể gửi mã OTP hay không
		private bool CanSendOtp(string identifier)
		{
			if (_otpSentTimes.ContainsKey(identifier))
			{
				var lastSentTime = _otpSentTimes[identifier];
				if (DateTime.Now - lastSentTime < _otpCooldownTime)
				{
					return false; // Nếu chưa đủ thời gian, không gửi mã
				}
			}
			return true;
		}

		public async Task<bool> SendVerificationCodeAsync(string identifier)
		{
			if (!CanSendOtp(identifier))
			{
				return false; // Nếu chưa đủ thời gian, không gửi mã
			}

			var content = new StringContent($"\"{identifier}\"", Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/SendVerificationCode", content);

			if (response.IsSuccessStatusCode)
			{
				// Cập nhật thời gian gửi mã OTP
				_otpSentTimes[identifier] = DateTime.Now;
				return true;
			}

			return false;
		}

		public async Task<bool> VerifyCodeAsync(string identifier, string code)
		{
			var payload = new
			{
				Identifier = identifier,
				Code = code
			};
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/VerifyCode", content);
			return response.IsSuccessStatusCode;
		}

		public async Task<string?> RegisterAsync(string identifier, string password, string confirmPassword)
		{
			var payload = new
			{
				Identifier = identifier,
				Password = password,
				ConfirmPassword = confirmPassword
			};
			var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
			var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/Register", content);

			return response.IsSuccessStatusCode
				? null
				: await response.Content.ReadAsStringAsync();
		}

		public async Task<LoginResult?> LoginAsync(string identifier, string password)
		{
			var payload = new
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

		public class Register
		{
			public string Identifier { get; set; }
			public string Password { get; set; }
			public string ConfirmPassword { get; set; }
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
