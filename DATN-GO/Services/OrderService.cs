using DATN_GO.ViewModels;
using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using DATN_GO.ViewModels.Orders;
using static System.Net.WebRequestMethods;

namespace DATN_GO.Service
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public OrderService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config.GetValue<string>("ApiSettings:BaseUrl")?.TrimEnd('/') + "/";
        }

        // Lấy tất cả đơn hàng kiểu ViewModel
        public async Task<List<OrderViewModel>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_baseUrl + "orders");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<OrderViewModel>>();
        }

        // Lấy đơn hàng theo Id kiểu ViewModel
        public async Task<OrderViewModel> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}orders/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<OrderViewModel>();
        }

        // Tạo mới (giữ nguyên) - nếu API trả entity thì bạn có thể map sang ViewModel khi cần
        public async Task<OrderViewModel> CreateAsync(OrderViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}orders", model);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<OrderViewModel>();
        }

        // Cập nhật đơn hàng (nên theo ViewModel hoặc DTO tương ứng)
        public async Task<bool> UpdateAsync(int id, OrderViewModel model)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}orders/{id}", model);
            return response.IsSuccessStatusCode;
        }

        // Xóa
        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}orders/{id}");
            return response.IsSuccessStatusCode;
        }

        // Lấy tất cả đơn theo UserId (storeUser)
        public async Task<(bool Success, List<OrderViewModel> Data, string Message)> GetOrdersByStoreUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/all-by-store/{userId}");
                if (!response.IsSuccessStatusCode)
                    return (false, null, $"Lỗi {response.StatusCode}");

                var data = await response.Content.ReadFromJsonAsync<List<OrderViewModel>>();
                return (true, data, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
        public async Task<OrderViewModel?> GetOrderDetailByIdAsync(int orderId, int userId)
        {
            try
            {
                // Gọi endpoint API có truyền cả userId để kiểm tra quyền
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/{orderId}/user/{userId}");
                if (!response.IsSuccessStatusCode)
                    return null;

                var order = await response.Content.ReadFromJsonAsync<OrderViewModel>();
                return order;
            }
            catch
            {
                return null;
            }
        }
        // Lấy chi tiết đơn hàng theo orderId (danh sách order detail ViewModel)
        // Nếu API trả về dạng List<OrderDetailsViewModel> hoặc có thể map như sau
        public async Task<(bool Success, List<OrderDetailViewModel> Data, string Message)> GetOrderDetailsByOrderIdAsync(int orderId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/{orderId}");
                // theo api của bạn, lấy chi tiết 1 order luôn trả cả details, bạn nên lấy một order rồi lấy order.OrderDetails ở client
                if (!response.IsSuccessStatusCode)
                    return (false, null, $"Lỗi {response.StatusCode}");

                var order = await response.Content.ReadFromJsonAsync<OrderViewModel>();
                if (order == null)
                    return (false, null, "Không tìm thấy đơn");

                return (true, order.OrderDetails ?? new List<OrderDetailViewModel>(), null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(int id, string newStatus)
        {
            try
            {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}orders/updatestatus/{id}?status={newStatus}");
                var response = await _httpClient.SendAsync(request);
                var message = await response.Content.ReadAsStringAsync();

                return (response.IsSuccessStatusCode, message);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }
        public async Task<(bool Success, List<OrderViewModel> Data, string Message)> GetOrdersByUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/user/{userId}");
                if (!response.IsSuccessStatusCode)
                    return (false, null, $"Lỗi {response.StatusCode}");

                var data = await response.Content.ReadFromJsonAsync<List<OrderViewModel>>();
                return (true, data, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, Statistics Data, string Message)> GetStatisticsAsync(DateTime? start, DateTime? end)
        {
            try
            {
                int storeId = 1;  // cần thiết update dynamic nếu bạn lưu store ở client
                var url = $"{_baseUrl}orders/statistics?storeId={storeId}";

                if (start.HasValue)
                    url += $"&start={start.Value:O}";
                if (end.HasValue)
                    url += $"&end={end.Value:O}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return (false, null, $"Lỗi {response.StatusCode}");

                var data = await response.Content.ReadFromJsonAsync<Statistics>();
                return (true, data, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
        public async Task<OrderDetailVM?> GetDetailAsync(int orderId)
        {
            var res = await _httpClient.GetAsync($"{_baseUrl}orders/{orderId}/detail");
            if (!res.IsSuccessStatusCode) return null;

            return await res.Content.ReadFromJsonAsync<OrderDetailVM>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

    }
}