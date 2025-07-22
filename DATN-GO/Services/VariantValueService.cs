using DATN_GO.Models;
using DATN_GO.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DATN_GO.Service
{
    public class VariantValueService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VariantValueService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<VariantValues>?> GetAllAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}VariantValues");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<VariantValues>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy danh sách VariantValues: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<VariantValues?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}VariantValues/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VariantValues>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy VariantValue ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<VariantValues?> CreateAsync(VariantValues model)
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}VariantValues", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VariantValues>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi tạo VariantValue: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdateAsync(int id, VariantValues model)
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}VariantValues/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi cập nhật VariantValue ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}VariantValues/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi xoá VariantValue ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<List<VariantValues>?> GetByVariantIdAsync(int variantId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}VariantValues/Variant/{variantId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<VariantValues>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy VariantValues theo VariantId {variantId}: {response.StatusCode}");
            return null;
        }
        public async Task<List<VariantDisplayGroup>> GetVariantDisplayByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/grouped/by-product/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<VariantDisplayGroup>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return new List<VariantDisplayGroup>();
        }

    }
}
