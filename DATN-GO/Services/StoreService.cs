using DATN_GO.Models;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DATN_GO.Service
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

        public async Task<List<Stores>?> GetAllStoresAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error fetching stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var stores = JsonSerializer.Deserialize<List<Stores>>(json, options);

            if (stores == null)
            {
                Console.WriteLine("[DEBUG] Deserialize store thất bại");
                return null;
            }

            return stores;
        }

        public async Task<Stores?> GetStoreByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Stores>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Store with ID {id} not found.");
            }
            else
            {
                Console.WriteLine($"Error fetching store by ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            return null;
        }

        public async Task<Stores?> CreateStoreAsync(Stores store)
        {
            var content = new StringContent(JsonSerializer.Serialize(store), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Stores", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Stores>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            Console.WriteLine($"Error creating store: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        public async Task<bool> UpdateStoreAsync(int id, Stores store)
        {
            var content = new StringContent(JsonSerializer.Serialize(store), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Stores/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Error updating store with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
        }

        public async Task<bool> DeleteStoreAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Stores/{id}");

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            Console.WriteLine($"Error deleting store with ID {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return false;
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

        public async Task<int> GetTotalStoresAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(json, out int total))
                    {
                        return total;
                    }
                }
                Console.WriteLine($"Error fetching total stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy tổng cửa hàng: {ex.Message}");
            }
            return 0;
        }

        public async Task<int> GetTotalActiveStoresAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/active");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(json, out int total))
                    {
                        return total;
                    }
                }
                Console.WriteLine($"Error fetching active stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy tổng cửa hàng đang hoạt động: {ex.Message}");
            }
            return 0;
        }
        public async Task<int> GetStoreCountByMonthYearAsync(int month, int year)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/by-month-year?month={month}&year={year}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (int.TryParse(json, out int total))
                    {
                        return total;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy số lượng cửa hàng tháng {month}/{year}: {ex.Message}");
            }
            return 0;
        }
        public async Task<Dictionary<int, int>> GetStoreCountByYearAsync(int year)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Stores/count/by-month/{year}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<int, int>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new Dictionary<int, int>();
                }

                Console.WriteLine($"Error fetching store count by year: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy dữ liệu theo năm: {ex.Message}");
            }

            return new Dictionary<int, int>();
        }
        // -- //

        public async Task<List<StoreQuantityViewModel>> GetStoreQuantitiesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/quantities");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] Failed to call Stores/quantities: {response.StatusCode}");
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<StoreQuantityViewModel>>(json, options) ?? new();
        }


        // Lấy chi tiết store cho admin
        public async Task<AdminStorelViewModels?> GetAdminDetailAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/admin/{id}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Lỗi lấy chi tiết admin store {id}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AdminStorelViewModels>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            });
        }

        // Lấy toàn bộ stores cho admin
        public async Task<List<AdminStorelViewModels>> GetAllAdminStoresAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores/admin");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Lỗi lấy danh sách admin stores: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return new();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AdminStorelViewModels>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            }) ?? new();
        }

        // Lấy rating từ reviews
        public async Task<Dictionary<int, (double AvgRating, int ReviewCount)>> GetRatingsByStoreUserAsync()
        {
            // 1) Reviews
            var rv = await _httpClient.GetAsync($"{_baseUrl}Reviews");
            if (!rv.IsSuccessStatusCode)
            {
                Console.WriteLine($"[REVIEWS] {rv.StatusCode} - {await rv.Content.ReadAsStringAsync()}");
                return new();
            }
            var reviews = JsonSerializer.Deserialize<List<Reviews>>(
                await rv.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            // 2) Products (để có StoreId)
            var pr = await _httpClient.GetAsync($"{_baseUrl}Products");
            var products = new List<Products>();
            if (pr.IsSuccessStatusCode)
            {
                products = JsonSerializer.Deserialize<List<Products>>(
                    await pr.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new();
            }
            else
            {
                Console.WriteLine($"[PRODUCTS] {pr.StatusCode} - {await pr.Content.ReadAsStringAsync()}");
            }

            // 3) Stores (để có UserId)
            var st = await _httpClient.GetAsync($"{_baseUrl}Stores");
            var stores = new List<Stores>();
            if (st.IsSuccessStatusCode)
            {
                stores = JsonSerializer.Deserialize<List<Stores>>(
                    await st.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new();
            }
            else
            {
                Console.WriteLine($"[STORES] {st.StatusCode} - {await st.Content.ReadAsStringAsync()}");
            }

            // 🔎 Debug count
            Console.WriteLine($"[DEBUG] reviews={reviews.Count}, products={products.Count}, stores={stores.Count}");

            // Build map để truy vấn O(1)
            var productId_to_storeId = products
                .Where(p => p != null)                           // phòng null
                .GroupBy(p => p.Id)
                .ToDictionary(g => g.Key, g => g.First().StoreId);  // ⬅️ yêu cầu Products phải có StoreId

            var storeId_to_userId = stores
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First().UserId);

            // Group theo UserId (chủ shop)
            var grouped = reviews
                .Where(r => r.Rating > 0)
                .Select(r =>
                {
                    if (!productId_to_storeId.TryGetValue(r.ProductId, out var storeId))
                        return (HasUser: false, UserId: -1, Rating: r.Rating);

                    if (!storeId_to_userId.TryGetValue(storeId, out var userId))
                        return (HasUser: false, UserId: -1, Rating: r.Rating);

                    return (HasUser: true, UserId: userId, Rating: r.Rating);
                })
                .Where(x => x.HasUser)
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        AvgRating: g.Average(x => x.Rating),
                        ReviewCount: g.Count()
                    )
                );

            Console.WriteLine($"[DEBUG] grouped_user_count={grouped.Count}");
            return grouped;
        }
        // Lấy tổng products
        public async Task<Dictionary<int, int>> GetTotalProductsByStoreAsync(bool onlyApproved = false)
        {
            var resp = await _httpClient.GetAsync($"{_baseUrl}Products");
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[PRODUCTS] {resp.StatusCode} - {await resp.Content.ReadAsStringAsync()}");
                return new();
            }

            var json = await resp.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<Products>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // 👇 QUAN TRỌNG: để đọc enum dạng string ("Approved") từ API
                Converters = { new JsonStringEnumConverter() }
            }) ?? new();

            // Nếu API trả enum là số (0/1) thì dòng trên vẫn ok.
            // Nếu muốn đếm tất, để onlyApproved = false
            if (onlyApproved)
                products = products.Where(p => p.Status == ProductStatus.Approved).ToList();

            var counts = products
                .GroupBy(p => p.StoreId)
                .ToDictionary(g => g.Key, g => g.Count());

            Console.WriteLine($"[DEBUG] products={products.Count}, groups={counts.Count}");
            return counts;
        }

        // Lấy tổng đơn hàng đã bán
        public async Task<Dictionary<int, int>> GetTotalSoldProductsByStoreAsync()
        {
            // 1) Reviews
            var rv = await _httpClient.GetAsync($"{_baseUrl}Reviews");
            if (!rv.IsSuccessStatusCode) return new();
            var reviews = JsonSerializer.Deserialize<List<Reviews>>(
                await rv.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            // 2) Products (để map ProductId -> StoreId)
            var pr = await _httpClient.GetAsync($"{_baseUrl}Products");
            if (!pr.IsSuccessStatusCode) return new();
            var products = JsonSerializer.Deserialize<List<Products>>(
                await pr.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();

            var prodToStore = products.ToDictionary(p => p.Id, p => p.StoreId);

            // 3) Gom theo StoreId và đếm số lượng review (mỗi review = 1 sp đã bán)
            var storeToSoldCount = new Dictionary<int, int>();

            foreach (var r in reviews.Where(x => x.Rating > 0))
            {
                if (!prodToStore.TryGetValue(r.ProductId, out var storeId)) continue;

                if (!storeToSoldCount.ContainsKey(storeId))
                    storeToSoldCount[storeId] = 0;

                storeToSoldCount[storeId] += 1;
            }

            return storeToSoldCount;
        }



    }
}