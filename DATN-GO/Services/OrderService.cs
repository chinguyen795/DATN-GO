using DATN_GO.ViewModels;
using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DATN_GO.ViewModels.Orders;

namespace DATN_GO.Service
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public OrderService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config.GetValue<string>("ApiSettings:BaseUrl")?.TrimEnd('/') + "/";
        }

        // ===== CRUD cơ bản (ViewModel tổng quát) =====

        public async Task<List<OrderViewModel>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_baseUrl + "orders");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<OrderViewModel>>(_jsonOpts);
        }

        public async Task<OrderViewModel?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}orders/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<OrderViewModel>(_jsonOpts);
        }

        public async Task<OrderViewModel?> CreateAsync(OrderViewModel model)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}orders", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrderViewModel>(_jsonOpts);
        }

        public async Task<bool> UpdateAsync(int id, OrderViewModel model)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}orders/{id}", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}orders/{id}");
            return response.IsSuccessStatusCode;
        }

        // ===== Danh sách theo store / user =====

        public async Task<(bool Success, List<OrderViewModel>? Data, string? Message)> GetOrdersByStoreUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/all-by-store/{userId}");
                if (!response.IsSuccessStatusCode) return (false, null, $"Lỗi {response.StatusCode}");
                var data = await response.Content.ReadFromJsonAsync<List<OrderViewModel>>(_jsonOpts);
                return (true, data ?? new List<OrderViewModel>(), null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, List<OrderViewModel>? Data, string? Message)> GetOrdersByUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/user/{userId}");
                if (!response.IsSuccessStatusCode) return (false, null, $"Lỗi {response.StatusCode}");
                var data = await response.Content.ReadFromJsonAsync<List<OrderViewModel>>(_jsonOpts);
                return (true, data ?? new List<OrderViewModel>(), null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, Statistics? Data, string? Message)> GetStatisticsAsync(DateTime? start, DateTime? end)
        {
            try
            {
                int storeId = 1;  // TODO: bind theo user đăng nhập nếu cần
                var url = $"{_baseUrl}orders/statistics?storeId={storeId}";
                if (start.HasValue) url += $"&start={start.Value:O}";
                if (end.HasValue) url += $"&end={end.Value:O}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (false, null, $"Lỗi {response.StatusCode}");
                var data = await response.Content.ReadFromJsonAsync<Statistics>(_jsonOpts);
                return (true, data, null);
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
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}orders/updatestatus/{id}?status={newStatus}");
                var response = await _httpClient.SendAsync(req);
                var message = await response.Content.ReadAsStringAsync();
                return (response.IsSuccessStatusCode, message);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }

        // =====================================================================
        //  PHẦN QUAN TRỌNG CHO TRANG CHI TIẾT ĐƠN (OrderUser/Detail)
        //  -> Trả về OrderDetailVM đã map sẵn DeliveryFee + LabelId + tính tổng
        // =====================================================================

        /// <summary>
        /// Lấy chi tiết đơn theo quyền user (API: GET /api/orders/{orderId}/user/{userId})
        /// Map sang OrderDetailVM cho View Razor.
        /// </summary>
        public async Task<OrderDetailVM?> GetOrderDetailByIdAsync(int orderId, int userId)
        {
            try
            {
                var res = await _httpClient.GetAsync($"{_baseUrl}orders/{orderId}/user/{userId}");
                if (!res.IsSuccessStatusCode) return null;

                var vm = await res.Content.ReadFromJsonAsync<OrderDetailVM>(
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (vm == null) return null;

                // Nếu API chưa set TotalPrice, tự cộng ItemsTotal + DeliveryFee để hiển thị
                if (vm.TotalPrice <= 0)
                    vm.TotalPrice = vm.ItemsTotal + vm.DeliveryFee;

                return vm;
            }
            catch
            {
                return null;
            }
        }



        /// <summary>
        /// (Tuỳ chọn) Lấy chi tiết theo endpoint chuyên biệt: GET /api/orders/{id}/detail
        /// Endpoint này trả thẳng OrderDetailDto -> map 1:1 sang OrderDetailVM.
        /// </summary>
        public async Task<OrderDetailVM?> GetDetailAsync(int orderId)
        {
            var res = await _httpClient.GetAsync($"{_baseUrl}orders/{orderId}/detail");
            if (!res.IsSuccessStatusCode) return null;

            var vm = await res.Content.ReadFromJsonAsync<OrderDetailVM>(_jsonOpts);
            if (vm == null) return null;

            // Fallback an toàn: nếu TotalPrice = 0 thì tự cộng
            if (vm.TotalPrice <= 0)
            {
                var itemsTotal = vm.Items?.Sum(i => i.SubTotal) ?? 0m;
                vm.TotalPrice = itemsTotal + vm.DeliveryFee;
            }
            return vm;
        }

        // ===== Phần cũ: lấy chi tiết qua OrderViewModel (không dùng cho view Detail) =====

        public async Task<(bool Success, List<OrderDetailViewModel>? Data, string? Message)> GetOrderDetailsByOrderIdAsync(int orderId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}orders/{orderId}");
                if (!response.IsSuccessStatusCode) return (false, null, $"Lỗi {response.StatusCode}");

                var order = await response.Content.ReadFromJsonAsync<OrderViewModel>(_jsonOpts);
                if (order == null) return (false, null, "Không tìm thấy đơn");

                return (true, order.OrderDetails ?? new List<OrderDetailViewModel>(), null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}