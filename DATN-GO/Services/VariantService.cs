using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DATN_GO.Service
{
    public class VariantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VariantService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Variants>?> GetAllAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Variants");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Variants>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy danh sách variants: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Variants?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Variants/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Variants>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy variant ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Variants?> CreateAsync(Variants model)
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Variants", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Variants>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi tạo variant: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdateAsync(int id, Variants model)
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Variants/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi cập nhật variant ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Variants/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi xoá variant ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<List<Variants>?> GetByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Variants/Product/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Variants>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy variants theo ProductId {productId}: {response.StatusCode}");
            return null;
        }

    }
}
