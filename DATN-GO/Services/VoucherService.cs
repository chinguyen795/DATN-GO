using DATN_GO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DATN_GO.Service
{
    public class VoucherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VoucherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        public async Task<bool> CreateVoucherAsync(Vouchers voucher)
        {
            var content = new StringContent(JsonSerializer.Serialize(voucher), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_baseUrl}Vouchers", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Categories>> GetAllCategoriesAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Categories");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Categories>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<List<Stores>> GetAllStoresAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Stores>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<bool> UpdateVoucherAsync(Vouchers voucher)
        {
            var content = new StringContent(JsonSerializer.Serialize(voucher), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_baseUrl}Vouchers/{voucher.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Vouchers>?> GetAllVouchersAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Vouchers");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Vouchers>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }
        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}Vouchers/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<dynamic> GetStoreInfoByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Stores?userId={userId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine("API Response: " + json);

                try
                {
                    var stores = JsonSerializer.Deserialize<List<Stores>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var store = stores?.FirstOrDefault();

                    if (store != null)
                    {
                        return new
                        {
                            StoreId = store.Id,
                            StoreName = store.Name
                        };
                    }

                    return new
                    {
                        StoreId = 0,
                        StoreName = "Chưa có tên cửa hàng"
                    };
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("Error deserializing JSON: " + ex.Message);
                    return new
                    {
                        StoreId = 0,
                        StoreName = "Chưa có tên cửa hàng"
                    };
                }
            }
            return new
            {
                StoreId = 0,
                StoreName = "Chưa có tên cửa hàng"
            };
        }

        public async Task<List<Vouchers>> GetVouchersByStoreOrAdminAsync(int? storeId)
        {
            // 1) Chuẩn hóa base url (đảm bảo có / ở cuối)
            var baseUrl = _baseUrl?.Trim() ?? string.Empty;
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            // 2) Helper gọi và parse JSON
            async Task<List<Vouchers>> FetchListAsync(string url)
            {
                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return new List<Vouchers>();
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Vouchers>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Vouchers>();
            }

            // 3) ADMIN: storeId == null
            if (storeId == null)
            {
                // Thử endpoint chuyên biệt (VIẾT HOA Vouchers vì bạn dùng vậy ở chỗ khác)
                var tryAdmin = await FetchListAsync($"{baseUrl}Vouchers/admin");
                if (tryAdmin.Count > 0) return tryAdmin;

                // Fallback: lấy tất cả rồi lọc StoreId == null
                var all = await FetchListAsync($"{baseUrl}Vouchers");
                return all.Where(v => v.StoreId == null).ToList();
            }
            else
            {
                // 4) SHOP: storeId != null
                var tryShop = await FetchListAsync($"{baseUrl}Vouchers/shop/{storeId.Value}");
                if (tryShop.Count > 0) return tryShop;

                // Fallback: lấy tất cả rồi lọc theo StoreId
                var all = await FetchListAsync($"{baseUrl}Vouchers");
                return all.Where(v => v.StoreId == storeId.Value).ToList();
            }
        }


        public class SaveVoucherServiceResult
        {
            public bool Ok { get; set; }
            public string Message { get; set; } = "";
        }

        public class SaveVoucherRequestDto
        {
            public int UserId { get; set; }
            public int VoucherId { get; set; }
            public SaveVoucherRequestDto() { }
            public SaveVoucherRequestDto(int userId, int voucherId) { UserId = userId; VoucherId = voucherId; }
        }

        public async Task<SaveVoucherServiceResult> SaveAdminVoucherAsync(int userId, int voucherId)
        {
            var url = $"{_baseUrl}UserVouchers/save";
            try
            {
                var resp = await _httpClient.PostAsJsonAsync(url, new SaveVoucherRequestDto(userId, voucherId));
                var body = await resp.Content.ReadAsStringAsync();

                Console.WriteLine($"[SaveAdminVoucherAsync] {url} => {(int)resp.StatusCode} {resp.ReasonPhrase}");
                Console.WriteLine($"[SaveAdminVoucherAsync] Body: {body}");

                if (resp.IsSuccessStatusCode)
                    return new SaveVoucherServiceResult { Ok = true, Message = TryExtractMessage(body) ?? "Lưu voucher thành công" };
                else
                    return new SaveVoucherServiceResult { Ok = false, Message = TryExtractMessage(body) ?? "Không lưu được voucher" };
            }
            catch (Exception ex)
            {
                return new SaveVoucherServiceResult { Ok = false, Message = $"Lỗi kết nối API: {ex.Message}" };
            }

            static string? TryExtractMessage(string json)
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
                }
                catch { }
                return null;
            }
        }

        public async Task<List<(int id, int voucherId)>> GetUserVouchersAsync(int userId)
        {
            var baseUrl = _baseUrl?.TrimEnd('/') + "/";
            var resp = await _httpClient.GetAsync($"{baseUrl}UserVouchers/user/{userId}");
            if (!resp.IsSuccessStatusCode) return new();
            var json = await resp.Content.ReadAsStringAsync();

            // Dạng trả về từ API: [{ id, userId, voucherId, ... }]
            using var doc = JsonDocument.Parse(json);
            var list = new List<(int id, int voucherId)>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var id = el.GetProperty("id").GetInt32();
                var voucherId = el.GetProperty("voucherId").GetInt32();
                list.Add((id, voucherId));
            }
            return list;
        }


    }

        
}