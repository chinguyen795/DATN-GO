using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DATN_GO.Models;
using DATN_GO.ViewModels;
using Microsoft.Extensions.Configuration;

namespace DATN_GO.Service
{
    public class PriceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public PriceService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] + "Prices";
        }

        public async Task<List<Prices>?> GetAllPricesAsync()
        {
            var response = await _httpClient.GetAsync(_baseUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Prices>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            Console.WriteLine($"Lỗi khi lấy danh sách giá: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Prices?> GetPriceByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Prices>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            Console.WriteLine($"Lỗi khi lấy giá ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Prices?> CreatePriceAsync(Prices price)
        {
            var content = new StringContent(JsonSerializer.Serialize(price), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_baseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Prices>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            Console.WriteLine($"Lỗi khi tạo giá: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdatePriceAsync(int id, Prices price)
        {
            var content = new StringContent(JsonSerializer.Serialize(price), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            Console.WriteLine($"Lỗi khi cập nhật giá ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeletePriceAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            Console.WriteLine($"Lỗi khi xoá giá ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }
        public async Task<decimal?> GetMinPriceByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/min-price/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                if (decimal.TryParse(json, out var price))
                {
                    return price;
                }
            }

            Console.WriteLine($"Lỗi khi lấy giá nhỏ nhất của sản phẩm ID {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<MinMaxPriceResponse?> GetMinMaxPriceByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/min-max-price/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MinMaxPriceResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            Console.WriteLine($"Lỗi khi lấy giá min-max của sản phẩm ID {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<decimal?> GetPriceByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/product/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("price", out var priceElement))
                {
                    return priceElement.GetDecimal();
                }
            }

            Console.WriteLine($"Lỗi khi lấy giá theo ProductId {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

    }
}
