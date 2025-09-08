using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DATN_GO.Service
{
    public class VoucherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public VoucherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = NormalizeBaseUrl(configuration["ApiSettings:BaseUrl"]);
        }

        private static string NormalizeBaseUrl(string? raw)
        {
            var s = (raw ?? string.Empty).Trim();
            if (!s.EndsWith("/")) s += "/";
            return s;
        }

        // ----------------------- Models phụ trợ (DTO) -----------------------
        public class StoreInfoDto
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; } = "";
        }

        // ----------------------- Helpers -----------------------
        private async Task<T?> GetJsonAsync<T>(string url)
        {
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return default;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        private async Task<List<T>> GetJsonListAsync<T>(string url)
        {
            var data = await GetJsonAsync<List<T>>(url);
            return data ?? new List<T>();
        }

        // ----------------------- Vouchers CRUD -----------------------
        public async Task<bool> CreateVoucherAsync(Vouchers voucher)
        {
            var content = new StringContent(JsonSerializer.Serialize(voucher), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync($"{_baseUrl}Vouchers", content);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateVoucherAsync(Vouchers voucher)
        {
            var content = new StringContent(JsonSerializer.Serialize(voucher), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PutAsync($"{_baseUrl}Vouchers/{voucher.Id}", content);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var resp = await _httpClient.DeleteAsync($"{_baseUrl}Vouchers/{id}");
            return resp.IsSuccessStatusCode;
        }

        public async Task<List<Vouchers>> GetAllVouchersAsync()
            => await GetJsonListAsync<Vouchers>($"{_baseUrl}Vouchers");

        // Dùng đúng các endpoint đã có ở API (admin / shop/{id})
        public async Task<List<Vouchers>> GetVouchersByStoreOrAdminAsync(int? storeId)
        {
            if (storeId == null)
            {
                var admin = await GetJsonListAsync<Vouchers>($"{_baseUrl}Vouchers/admin");
                if (admin.Count > 0) return admin;

                var all = await GetAllVouchersAsync();
                return all.Where(v => v.StoreId == null).ToList();
            }
            else
            {
                var shop = await GetJsonListAsync<Vouchers>($"{_baseUrl}Vouchers/shop/{storeId.Value}");
                if (shop.Count > 0) return shop;

                var all = await GetAllVouchersAsync();
                return all.Where(v => v.StoreId == storeId.Value).ToList();
            }
        }

        // ----------------------- Master data -----------------------
        // Category: dùng chung toàn hệ thống
        public async Task<List<Categories>> GetAllCategoriesAsync()
            => await GetJsonListAsync<Categories>($"{_baseUrl}Categories");

        public async Task<List<Stores>> GetAllStoresAsync()
            => await GetJsonListAsync<Stores>($"{_baseUrl}Stores");

        // Admin: toàn bộ sản phẩm
        public async Task<List<Products>> GetAllProductsAsync()
        {
            var baseUrl = _baseUrl?.TrimEnd('/') + "/";
            var resp = await _httpClient.GetAsync($"{baseUrl}Products/admin");
            if (!resp.IsSuccessStatusCode)
            {
                resp = await _httpClient.GetAsync($"{baseUrl}Products");
                if (!resp.IsSuccessStatusCode) return new List<Products>();
            }
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Products>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Products>();
        }

        // ✅ Seller: chỉ sản phẩm thuộc shop
        public async Task<List<Products>> GetProductsByStoreAsync(int storeId)
            => await GetJsonListAsync<Products>($"{_baseUrl}Products/store/{storeId}");

        // ----------------------- Store Info -----------------------
        public async Task<StoreInfoDto> GetStoreInfoByUserIdAsync(int userId)
        {
            var url = $"{_baseUrl}Stores?userId={userId}";
            try
            {
                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                    return new StoreInfoDto { StoreId = 0, StoreName = "Chưa có tên cửa hàng" };

                var json = await resp.Content.ReadAsStringAsync();

                // Duyệt mảng JSON & tìm phần tử có owner trùng userId
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return new StoreInfoDto { StoreId = 0, StoreName = "Chưa có tên cửa hàng" };

                JsonElement? chosen = null;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    bool isMatch = false;

                    // Match theo các field "thường gặp"
                    if (el.TryGetProperty("userId", out var u) && u.ValueKind == JsonValueKind.Number && u.GetInt32() == userId)
                        isMatch = true;
                    else if (el.TryGetProperty("ownerUserId", out var ou) && ou.ValueKind == JsonValueKind.Number && ou.GetInt32() == userId)
                        isMatch = true;
                    else if (el.TryGetProperty("createdByUserId", out var cu) && cu.ValueKind == JsonValueKind.Number && cu.GetInt32() == userId)
                        isMatch = true;

                    if (isMatch) { chosen = el; break; }
                }

                // Nếu API không lọc, còn nhiều store -> chọn cái có match; nếu vẫn không có, dùng cái đầu
                var target = chosen ?? (doc.RootElement.EnumerateArray().FirstOrDefault() is JsonElement x ? x : default);

                if (target.ValueKind == JsonValueKind.Object)
                {
                    int id = target.TryGetProperty("id", out var pid) && pid.ValueKind == JsonValueKind.Number ? pid.GetInt32() : 0;
                    string name = target.TryGetProperty("name", out var pn) && pn.ValueKind == JsonValueKind.String ? pn.GetString()! : $"Cửa hàng #{id}";
                    return new StoreInfoDto { StoreId = id, StoreName = name };
                }
            }
            catch
            {
                // ignore & fall through
            }

            return new StoreInfoDto { StoreId = 0, StoreName = "Chưa có tên cửa hàng" };
        }


        // ----------------------- User Vouchers (optional) -----------------------
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

                string? TryExtractMessage(string json)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
                    }
                    catch { }
                    return null;
                }

                if (resp.IsSuccessStatusCode)
                    return new SaveVoucherServiceResult { Ok = true, Message = TryExtractMessage(body) ?? "Lưu voucher thành công" };
                else
                    return new SaveVoucherServiceResult { Ok = false, Message = TryExtractMessage(body) ?? "Không lưu được voucher" };
            }
            catch (Exception ex)
            {
                return new SaveVoucherServiceResult { Ok = false, Message = $"Lỗi kết nối API: {ex.Message}" };
            }
        }
        public async Task<List<Products>> GetProductsByStoreIdAsync(int storeId)
    => await GetJsonListAsync<Products>($"{_baseUrl}Products/store/{storeId}");


        public async Task<List<(int id, int voucherId)>> GetUserVouchersAsync(int userId)
        {
            var url = $"{_baseUrl}UserVouchers/user/{userId}";
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return new();

            var json = await resp.Content.ReadAsStringAsync();
            var list = new List<(int id, int voucherId)>();
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var id = el.GetProperty("id").GetInt32();
                    var voucherId = el.GetProperty("voucherId").GetInt32();
                    list.Add((id, voucherId));
                }
            }
            catch { }

            return list;
        }
    }
}