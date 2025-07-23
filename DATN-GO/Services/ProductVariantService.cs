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
    public class ProductVariantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ProductVariantService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<ProductVariants>?> GetAllAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}ProductVariants");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ProductVariants>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy danh sách biến thể: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<ProductVariants?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}ProductVariants/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductVariants>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy biến thể ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<ProductVariants?> CreateAsync(ProductVariants model)
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}ProductVariants", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductVariants>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi tạo biến thể: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdateAsync(int id, ProductVariants model)
        {
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}ProductVariants/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi cập nhật biến thể ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}ProductVariants/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi xoá biến thể ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<List<ProductVariants>?> GetByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}ProductVariants/GetByProductId/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ProductVariants>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy biến thể theo ProductId {productId}: {response.StatusCode}");
            return null;
        }
        public async Task<List<string>?> GetImagesByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}ProductVariant/images/{productId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            Console.WriteLine($"Lỗi khi lấy danh sách hình ảnh theo ProductId {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<List<VariantCombinationViewModel>?> GetVariantCombinationsByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}VariantComposition/combinations/{productId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<VariantCombinationViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            Console.WriteLine($"Lỗi khi lấy combinations của sản phẩm {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }


    }
}
