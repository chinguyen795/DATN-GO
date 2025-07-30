using DATN_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task<List<Categories>> GetAllCategoriesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Categories");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Categories>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<List<Stores>> GetAllStoresAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Stores>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<bool> UpdateVoucherAsync(Vouchers voucher)
        {
            var content = new StringContent(JsonSerializer.Serialize(voucher), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Vouchers/{voucher.Id}", content);
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

        public async Task<dynamic> GetStoreInfoByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores?userId={userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine("API Response: " + json);

                try
                {
                    var stores = JsonSerializer.Deserialize<List<Stores>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var store = stores?.FirstOrDefault();

                    if (store != null)
                    {
                        return new
                        {
                            StoreId = store.Id,
                            StoreName = store.Name
                        };
                    }

                    return new
                    {
                        StoreId = 0,
                        StoreName = "Chưa có tên cửa hàng"
                    };
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("Error deserializing JSON: " + ex.Message);
                    return new
                    {
                        StoreId = 0,
                        StoreName = "Chưa có tên cửa hàng"
                    };
                }
            }
            return new
            {
                StoreId = 0,
                StoreName = "Chưa có tên cửa hàng"
            };
        }








    }
}