using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DATN_GO.Models;

namespace DATN_GO.Controllers
{
    public class AdminVouchersController : Controller
    {
        private readonly HttpClient _http;
        private readonly string _apiBase;

        public AdminVouchersController(IConfiguration config)
        {
            _apiBase = config["ApiOptions:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:7096";
            _http = new HttpClient { BaseAddress = new Uri(_apiBase + "/") };
            // Nếu API cần Bearer token của user:
            // var token = ...; _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private int GetCurrentUserId()
        {
            var idStr = HttpContext.Session.GetString("Id");
            return int.TryParse(idStr, out var uid) ? uid : 0;
        }


        // DTOs khớp với API
        public enum VoucherStatus { Valid, Expired, Used, Saved }
        public enum VoucherType { Platform, Shop }


        // từ API GET /api/UserVouchers/user/{userId}?scope=platform
        private sealed class UserVoucherItem
        {
            public int id { get; set; }            // userVoucherId
            public int userId { get; set; }
            public int voucherId { get; set; }
            public string savedAt { get; set; } = "";
            public bool isUsed { get; set; }
            public VoucherObj voucher { get; set; } = new();
            public sealed class VoucherObj
            {
                public int id { get; set; }
                public decimal reduce { get; set; }
                public string type { get; set; } = "";
                public decimal minOrder { get; set; }
                public string startDate { get; set; } = "";
                public string endDate { get; set; } = "";
                public string status { get; set; } = "";
                public int? storeId { get; set; }
                public string storeName { get; set; } = "";
            }
        }

        private sealed class SaveVoucherRequest { public int UserId { get; set; } public int VoucherId { get; set; } }
        private sealed class ApiOk { public bool ok { get; set; } public string message { get; set; } = ""; public int id { get; set; } }
        private sealed class ApiErr { public bool ok { get; set; } public string message { get; set; } = ""; public string? error { get; set; } }

        // GET: /AdminVouchers
        public async Task<IActionResult> Index()
        {
            // 1) Lấy toàn bộ voucher admin
            var res = await _http.GetAsync("api/vouchers/admin");
            if (!res.IsSuccessStatusCode)
            {
                ViewBag.Error = $"API lỗi {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}";
                return View(new List<Vouchers>());
            }
            var vouchers = await res.Content.ReadFromJsonAsync<List<Vouchers>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                           ?? new List<Vouchers>();

            // 2) Nếu đã đăng nhập: lấy list voucher user đã lưu (platform) để đánh dấu nút & map userVoucherId
            var uid = GetCurrentUserId();
            var savedSet = new HashSet<int>();                      // voucherId đã lưu
            var mapVoucherToUserVoucher = new Dictionary<int, int>(); // voucherId -> userVoucherId

            if (uid > 0)
            {
                var r2 = await _http.GetAsync($"api/UserVouchers/user/{uid}?scope=platform");
                if (r2.IsSuccessStatusCode)
                {
                    var savedItems = await r2.Content.ReadFromJsonAsync<List<UserVoucherItem>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                                   ?? new List<UserVoucherItem>();
                    foreach (var it in savedItems)
                    {
                        savedSet.Add(it.voucherId);
                        mapVoucherToUserVoucher[it.voucherId] = it.id; // map để dùng voucher/unsave
                    }
                    ViewBag.SavedCount = savedItems.Count;
                }
                else
                {
                    ViewBag.SavedCount = 0;
                }
            }
            else
            {
                ViewBag.SavedCount = 0;
            }

            // 3) Thống kê
            var nowUtc = DateTime.UtcNow;
            var total = vouchers.Count;
            var valid = vouchers.Count(x => nowUtc <= x.EndDate);
            var maxPercent = vouchers.Where(x => x.IsPercentage).Select(x => x.Reduce).DefaultIfEmpty(0).Max();

            ViewBag.Total = total;
            ViewBag.Valid = valid;
            ViewBag.MaxPercent = maxPercent;
            ViewBag.UserId = uid;
            ViewBag.Saved = savedSet; // HashSet<int>
            ViewBag.VoucherMap = mapVoucherToUserVoucher; // Dictionary<int,int>

            return View(vouchers);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] int voucherId)
        {
            var uid = GetCurrentUserId();
            if (uid <= 0)
                return Unauthorized(new ApiErr { ok = false, message = "Bạn cần đăng nhập để lưu voucher." });

            var payload = new SaveVoucherRequest { UserId = uid, VoucherId = voucherId };
            var res = await _http.PostAsJsonAsync("api/UserVouchers/save", payload);
            var content = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                // API trả về ok -> giữ nguyên nội dung (thường là JSON ApiOk)
                return Content(content, "application/json");
            }

            // Nếu lỗi, parse về ApiErr
            try
            {
                var err = JsonSerializer.Deserialize<ApiErr>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return StatusCode((int)res.StatusCode,
                    err ?? new ApiErr { ok = false, message = content });
            }
            catch
            {
                return StatusCode((int)res.StatusCode,
                    new ApiErr { ok = false, message = content });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unsave([FromForm] int userVoucherId)
        {
            var uid = GetCurrentUserId();
            if (uid <= 0) return Unauthorized(new { ok = false, message = "Bạn cần đăng nhập." });

            var res = await _http.DeleteAsync($"api/UserVouchers/{userVoucherId}");
            var content = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
                return Content(content, "application/json");

            return StatusCode((int)res.StatusCode, new { ok = false, message = content });
        }

        // POST: /AdminVouchers/Use  (bắt buộc đăng nhập)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Use([FromForm] int userVoucherId)
        {
            var uid = GetCurrentUserId();
            if (uid <= 0) return Unauthorized(new { ok = false, message = "Bạn cần đăng nhập." });

            var req = new HttpRequestMessage(HttpMethod.Put, $"api/UserVouchers/use/{userVoucherId}");
            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
                return Content(content, "application/json");

            return StatusCode((int)res.StatusCode, new { ok = false, message = content });
        }
    }
}

