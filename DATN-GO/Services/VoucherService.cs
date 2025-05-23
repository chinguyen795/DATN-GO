using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using DATN_GO.Models;

namespace DATN_GO.Service
{
    public class VoucherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VoucherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<bool> CreateVoucherAsync(Vouchers voucher)
        {
            var content = new StringContent(JsonSerializer.Serialize(voucher), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Vouchers", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Vouchers>?> GetAllVouchersAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Vouchers");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Vouchers>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Vouchers/{id}");
            return response.IsSuccessStatusCode;
        }

    }
}
