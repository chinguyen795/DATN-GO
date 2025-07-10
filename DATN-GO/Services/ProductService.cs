using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DATN_GO.Models;
using Microsoft.Extensions.Configuration;

namespace DATN_GO.Service
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ProductService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Products>?> GetAllProductsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Products");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Products>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy danh sách sản phẩm: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Products?> GetProductByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Products/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Products>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy sản phẩm ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Products?> CreateProductAsync(Products product)
        {
            var content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Products", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Products>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi tạo sản phẩm: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdateProductAsync(int id, Products product)
        {
            var content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Products/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi cập nhật sản phẩm ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Products/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi xoá sản phẩm ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<List<Products>?> GetProductsByStoreIdAsync(int storeId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Products/Store/{storeId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Products>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                Console.WriteLine($"Lỗi khi lấy sản phẩm theo Store ID {storeId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception khi lấy sản phẩm theo Store ID: {ex.Message}");
                return null;
            }
        }
        public async Task<int> GetTotalProductsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Products/count/total");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return int.Parse(json);
            }
            return 0;
        }
        public async Task<Dictionary<int, int>> GetProductCountByMonthAsync(int year)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Products/count/by-month/{year}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<int, int>>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy số lượng sản phẩm theo tháng: {ex.Message}");
            }
            return new Dictionary<int, int>();
        }

    }
}
