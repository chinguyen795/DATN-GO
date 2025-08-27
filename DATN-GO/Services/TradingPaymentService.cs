using System.Net.Http.Json;
using System.Text.Json;
using DATN_GO.Models;
using DATN_GO.ViewModels;

namespace DATN_GO.Services
{
    public class TradingPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public TradingPaymentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<IEnumerable<TradingPayment>> GetAllAsync()
        {
            var payments = await _httpClient.GetFromJsonAsync<List<TradingPayment>>(
                $"{_baseUrl}TradingPayment"
            );

            return payments ?? new List<TradingPayment>();
        }

        public async Task<TradingPayment> GetByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<TradingPayment>($"{_baseUrl}TradingPayment/{id}");
        }

        public async Task<bool> CreateAsync(TradingPayment model)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}TradingPayment", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(TradingPayment model)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}TradingPayment/{model.Id}", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}TradingPayment/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ConfirmAsync(int id)
        {
            var response = await _httpClient.PutAsync($"{_baseUrl}TradingPayment/{id}/confirm", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RejectAsync(int id)
        {
            var response = await _httpClient.PutAsync($"{_baseUrl}TradingPayment/{id}/reject", null);
            return response.IsSuccessStatusCode;
        }
        public async Task<List<TradingPayment>> GetByStoreIdAsync(int storeId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}TradingPayment/store/{storeId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<TradingPayment>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new List<TradingPayment>();
        }
    }
}
