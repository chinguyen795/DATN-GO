using DATN_GO.Models;
using DATN_GO.ViewModels;
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

        // ================= DTO phụ trợ =================
        public class ServiceResult
        {
            public bool Ok { get; set; }
            public string Message { get; set; } = "";
        }

        public class StoreInfoDto
        {
            public int StoreId { get; set; }
            public string StoreName { get; set; } = "";
        }

        // ================= Helpers =================
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

        private static StringContent AsJsonContent<T>(T data)
        {
            return new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        }

        // ================= Vouchers CRUD =================
        public async Task<ServiceResult> CreateVoucherAsync(CreateVoucherDto dto)
        {
            var resp = await _httpClient.PostAsync($"{_baseUrl}Vouchers", AsJsonContent(dto));
            var msg = await resp.Content.ReadAsStringAsync();
            return new ServiceResult
            {
                Ok = resp.IsSuccessStatusCode,
                Message = resp.IsSuccessStatusCode ? "Thêm voucher thành công." : $"Thêm thất bại: {msg}"
            };
        }

        public async Task<ServiceResult> UpdateVoucherAsync(UpdateVoucherDto dto)
        {
            var resp = await _httpClient.PutAsync($"{_baseUrl}Vouchers/{dto.Id}", AsJsonContent(dto));
            var msg = await resp.Content.ReadAsStringAsync();
            return new ServiceResult
            {
                Ok = resp.IsSuccessStatusCode,
                Message = resp.IsSuccessStatusCode ? "Cập nhật voucher thành công." : $"Cập nhật thất bại: {msg}"
            };
        }

        public async Task<ServiceResult> DeleteVoucherAsync(int id)
        {
            var resp = await _httpClient.DeleteAsync($"{_baseUrl}Vouchers/{id}");
            return new ServiceResult
            {
                Ok = resp.IsSuccessStatusCode,
                Message = resp.IsSuccessStatusCode ? "Xóa voucher thành công." : "Xóa thất bại"
            };
        }

        public async Task<List<Vouchers>> GetAllVouchersAsync()
            => await GetJsonListAsync<Vouchers>($"{_baseUrl}Vouchers");

        public async Task<Vouchers?> GetVoucherByIdAsync(int id)
            => await GetJsonAsync<Vouchers>($"{_baseUrl}Vouchers/{id}");

        public async Task<List<Vouchers>> GetVouchersByStoreOrAdminAsync(int? storeId)
        {
            if (storeId == null)
            {
                var admin = await GetJsonListAsync<Vouchers>($"{_baseUrl}Vouchers/admin");
                if (admin.Count > 0) return admin;
                return (await GetAllVouchersAsync()).Where(v => v.StoreId == null).ToList();
            }
            else
            {
                var shop = await GetJsonListAsync<Vouchers>($"{_baseUrl}Vouchers/shop/{storeId.Value}");
                if (shop.Count > 0) return shop;
                return (await GetAllVouchersAsync()).Where(v => v.StoreId == storeId.Value).ToList();
            }
        }

        // ================= Master Data =================
        public async Task<List<Categories>> GetAllCategoriesAsync()
            => await GetJsonListAsync<Categories>($"{_baseUrl}Categories");

        public async Task<List<Stores>> GetAllStoresAsync()
            => await GetJsonListAsync<Stores>($"{_baseUrl}Stores");

        public async Task<List<Products>> GetAllProductsAsync()
        {
            var resp = await _httpClient.GetAsync($"{_baseUrl}Products/admin");
            if (!resp.IsSuccessStatusCode)
                resp = await _httpClient.GetAsync($"{_baseUrl}Products");

            if (!resp.IsSuccessStatusCode) return new List<Products>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Products>>(json, _jsonOpts) ?? new List<Products>();
        }

        public async Task<List<Products>> GetProductsByStoreAsync(int storeId)
            => await GetJsonListAsync<Products>($"{_baseUrl}Products/store/{storeId}");

        public async Task<VoucherService.StoreInfoDto> GetStoreInfoByUserIdAsync(int userId)
        {
            // Thử một loạt endpoint "thường gặp" – nếu backend có cái nào thì ăn ngay.
            foreach (var path in new[] {
        $"Stores/user/{userId}",
        $"Stores/by-user/{userId}",
        $"Stores/get-by-user/{userId}",
        $"Stores?userId={userId}" // cuối cùng mới thử query param
    })
            {
                var url = $"{_baseUrl}{path}";
                try
                {
                    var resp = await _httpClient.GetAsync(url);
                    if (!resp.IsSuccessStatusCode) continue;

                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (!BelongsToUser(root, userId)) continue;
                        return ToStoreInfoDto(root);
                    }

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        // TỰ LỌC client-side để đề phòng API phớt lờ query userId
                        var match = root.EnumerateArray().FirstOrDefault(el => BelongsToUser(el, userId));
                        if (match.ValueKind == JsonValueKind.Object)
                            return ToStoreInfoDto(match);
                    }
                }
                catch
                {
                    // bỏ qua, thử path kế tiếp
                }
            }

            // Fallback: quét toàn bộ /Stores rồi tự lọc
            try
            {
                var resp = await _httpClient.GetAsync($"{_baseUrl}Stores");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        var match = root.EnumerateArray().FirstOrDefault(el => BelongsToUser(el, userId));
                        if (match.ValueKind == JsonValueKind.Object)
                            return ToStoreInfoDto(match);
                    }
                    else if (root.ValueKind == JsonValueKind.Object && BelongsToUser(root, userId))
                    {
                        return ToStoreInfoDto(root);
                    }
                }
            }
            catch { }

            // KHÔNG đoán bừa storeId=1 nữa – trả về rỗng để controller handle.
            return new StoreInfoDto { StoreId = 0, StoreName = "" };

            // --- helpers ---
            static bool BelongsToUser(JsonElement el, int uid)
            {
                return (TryGetInt(el, "ownerUserId", out var o1) && o1 == uid)
                    || (TryGetInt(el, "userId", out var o2) && o2 == uid)
                    || (TryGetInt(el, "ownerId", out var o3) && o3 == uid);
            }

            static StoreInfoDto ToStoreInfoDto(JsonElement el)
            {
                return new StoreInfoDto
                {
                    StoreId = TryGetInt(el, "id", out var id) ? id : 0,
                    StoreName = TryGetString(el, "name", out var nm) ? nm : ""
                };
            }

            static bool TryGetInt(JsonElement el, string prop, out int v)
            {
                v = 0;
                return el.TryGetProperty(prop, out var p) &&
                       p.ValueKind == JsonValueKind.Number &&
                       p.TryGetInt32(out v);
            }

            static bool TryGetString(JsonElement el, string prop, out string v)
            {
                v = "";
                return el.TryGetProperty(prop, out var p) &&
                       p.ValueKind == JsonValueKind.String &&
                       (v = p.GetString() ?? "") != null;
            }
        }

        // Alias để controller mới không phải đổi tên hàm
        public Task<List<Products>> GetProductsByStoreIdAsync(int storeId)
            => GetProductsByStoreAsync(storeId);


        // ================= User Vouchers =================
        public async Task<ServiceResult> SaveAdminVoucherAsync(int userId, int voucherId)
        {
            var url = $"{_baseUrl}UserVouchers/save";
            try
            {
                var resp = await _httpClient.PostAsJsonAsync(url, new { UserId = userId, VoucherId = voucherId });
                var body = await resp.Content.ReadAsStringAsync();
                return new ServiceResult
                {
                    Ok = resp.IsSuccessStatusCode,
                    Message = resp.IsSuccessStatusCode ? "Lưu voucher thành công" : $"Không lưu được: {body}"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult { Ok = false, Message = $"Lỗi kết nối API: {ex.Message}" };
            }
        }

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
                    list.Add((el.GetProperty("id").GetInt32(), el.GetProperty("voucherId").GetInt32()));
                }
            }
            catch { }
            return list;
        }
    }
}