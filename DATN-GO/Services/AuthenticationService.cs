using System.Net.Http;
using System.Net.Http.Headers;
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

        public AuthenticationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<bool> SendVerificationCodeAsync(string identifier)
        {
            var content = new StringContent($"\"{identifier}\"", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Authentication/SendVerificationCode", content);
            return response.IsSuccessStatusCode;
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
