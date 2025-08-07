using DATN_GO.Models;
using DATN_GO.ViewModels.Cart;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DATN_GO.Service
{
    public class CartService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public CartService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

     public async Task<bool> AddToCartAsync(AddToCartRequest request)
{
    var json = JsonSerializer.Serialize(request);
    Console.WriteLine("➡️ JSON gửi đến API:");
    Console.WriteLine(json);

    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await _httpClient.PostAsync($"{_baseUrl}Cart/add", content);

    var responseContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"📥 Kết quả trả về: {response.StatusCode}");
    Console.WriteLine($"📥 Nội dung trả về: {responseContent}");

    return response.IsSuccessStatusCode;
}



        public async Task<CartSummaryViewModel?> GetCartByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Cart/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CartSummaryViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            Console.WriteLine($"Lỗi khi lấy giỏ hàng: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<List<ShippingGroupViewModel>?> GetShippingGroupsAsync(int userId, int addressId)
        {
            var body = new
            {
                UserId = userId,
                AddressId = addressId
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Cart/shipping-groups", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ShippingGroupViewModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return null;
        }





        public async Task<bool> RemoveFromCartAsync(int cartId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Cart/remove/{cartId}");
            Console.WriteLine($"Xoá cartId = {cartId}, status = {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateQuantityAsync(int cartId, int newQuantity)
        {
            var body = new
            {
                CartId = cartId,
                NewQuantity = newQuantity
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Cart/update-quantity", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSelectionAsync(List<int> selectedCartIds)
        {
            var json = JsonSerializer.Serialize(selectedCartIds);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Cart/update-selection", content);
            return response.IsSuccessStatusCode;
        }


    }
}
