using DATN_GO.Models;
using DATN_GO.ViewModels.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DATN_GO.Service
{
    public class StoreService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public StoreService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Stores>?> GetAllStoresAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var stores = JsonSerializer.Deserialize<List<Stores>>(json, options);

            if (stores == null)
            {
                Console.WriteLine("[DEBUG] Deserialize store thất bại");
                return null;
            }

            return stores;
        }

        public async Task<Stores?> GetStoreByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Stores>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Store with ID {id} not found.");
            }
            else
            {
                Console.WriteLine($"Error fetching store by ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            return null;
        }

        public async Task<Stores?> CreateStoreAsync(Stores store)
        {
            var content = new StringContent(JsonSerializer.Serialize(store), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Stores", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Stores>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Error creating store: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdateStoreAsync(int id, Stores store)
        {
            var content = new StringContent(JsonSerializer.Serialize(store), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Stores/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Error updating store with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteStoreAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Stores/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Error deleting store with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<Stores?> GetStoreByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Stores>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            Console.WriteLine($"❌ Lỗi khi lấy Store theo UserId {userId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<int> GetTotalStoresAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(json, out int total))
                    {
                        return total;
                    }
                }
                Console.WriteLine($"Error fetching total stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy tổng cửa hàng: {ex.Message}");
            }
            return 0;
        }

        public async Task<int> GetTotalActiveStoresAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/active");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(json, out int total))
                    {
                        return total;
                    }
                }
                Console.WriteLine($"Error fetching active stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy tổng cửa hàng đang hoạt động: {ex.Message}");
            }
            return 0;
        }
        public async Task<int> GetStoreCountByMonthYearAsync(int month, int year)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/by-month-year?month={month}&year={year}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(json, out int total))
                    {
                        return total;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy số lượng cửa hàng tháng {month}/{year}: {ex.Message}");
            }
            return 0;
        }
        public async Task<Dictionary<int, int>> GetStoreCountByYearAsync(int year)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/by-month/{year}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<int, int>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new Dictionary<int, int>();
                }

                Console.WriteLine($"Error fetching store count by year: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy dữ liệu theo năm: {ex.Message}");
            }

            return new Dictionary<int, int>();
        }
        // -- //

        public async Task<List<StoreQuantityViewModel>> GetStoreQuantitiesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/quantities");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] Failed to call Stores/quantities: {response.StatusCode}");
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<StoreQuantityViewModel>>(json, options) ?? new();
        }







    }
}