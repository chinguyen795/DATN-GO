using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DATN_GO.Models;      // Nếu có entity dùng trong client
using DATN_GO.ViewModels;  // Chứa ReviewCreateRequest
using Microsoft.Extensions.Configuration;

namespace DATN_GO.Service
{
    public class ReviewService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ReviewService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<List<Reviews>?> GetAllReviewsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Reviews");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Reviews>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy danh sách đánh giá: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Reviews?> GetReviewByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Reviews/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Reviews>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi lấy đánh giá ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<Reviews?> CreateReviewAsync(ReviewCreateRequest reviewRequest)
        {
            var content = new StringContent(JsonSerializer.Serialize(reviewRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Reviews/add", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Reviews>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Lỗi khi tạo đánh giá: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }


        public async Task<bool> UpdateReviewAsync(int id, Reviews review)
        {
            var content = new StringContent(JsonSerializer.Serialize(review), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Reviews/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi cập nhật đánh giá ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Reviews/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Lỗi khi xoá đánh giá ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<List<ReviewViewModel>?> GetReviewsByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Reviews/product/{productId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ReviewViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            Console.WriteLine($"Lỗi khi lấy review của product {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> HasUserReviewedProductAsync(int orderId, int productId, int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Reviews/has-review/{orderId}/product/{productId}/user/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            Console.WriteLine($"Lỗi khi kiểm tra review sản phẩm {productId} trong đơn {orderId} của user {userId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }



        public async Task<List<CompletedOrderViewModel>> GetCompletedOrdersByUserAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Reviews/completed-orders/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CompletedOrderViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<CompletedOrderViewModel>();
            }
            Console.WriteLine($"Lỗi khi lấy đơn hàng hoàn thành của user {userId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return new List<CompletedOrderViewModel>();
        }



    }
}