
using DATN_GO.Models;
using System.Net.Http.Json;

namespace DATN_GO.Services
{
    public class UserTradingPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public UserTradingPaymentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"]; // ví dụ: "https://localhost:5001/api/"
        }

        public async Task<IEnumerable<UserTradingPayment>> GetAllAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<UserTradingPayment>>(
                $"{_baseUrl}UserTradingPayments") ?? new List<UserTradingPayment>();
        }

        public async Task<UserTradingPayment?> GetByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<UserTradingPayment>(
                $"{_baseUrl}UserTradingPayments/{id}");
        }

        public async Task<UserTradingPayment?> CreateAsync(UserTradingPayment payment)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}UserTradingPayments", payment);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<UserTradingPayment>();
            return null;
        }

        public async Task<UserTradingPayment?> UpdateAsync(int id, UserTradingPayment payment)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}UserTradingPayments/{id}", payment);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<UserTradingPayment>();
            return null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}UserTradingPayments/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RejectAsync(int id)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}UserTradingPayments/{id}/reject", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ConfirmAsync(int id)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}UserTradingPayments/{id}/confirm", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<UserTradingPayment>> GetByUserIdAsync(int userId)
        {
            return await _httpClient.GetFromJsonAsync<List<UserTradingPayment>>(
                $"{_baseUrl}UserTradingPayments/user/{userId}") ?? new List<UserTradingPayment>();
        }
    }
}