using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DATN_GO.Services
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

    }
}
