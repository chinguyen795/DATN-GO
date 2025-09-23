using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN_GO.Controllers
{
    [AutoValidateAntiforgeryToken] // dùng với AJAX header RequestVerificationToken
    public class VoucherAdminController : Controller
    {

        private readonly StoreService _storeService;
        private readonly VoucherService _voucherService;
        private readonly IConfiguration _configuration;

        public VoucherAdminController(VoucherService voucherService, IConfiguration configuration, StoreService storeService)
        {
            _voucherService = voucherService;
            _configuration = configuration;
            _storeService = storeService;
        }
        private int GetCurrentUserId()
        {
            var idStr = HttpContext.Session.GetString("Id");
            return int.TryParse(idStr, out var uid) ? uid : 0;

        }
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? sort, int page = 1, int pageSize = 8)
        {
            var vouchers = await _voucherService.GetVouchersByStoreOrAdminAsync(null) ?? new List<Vouchers>();
            vouchers = vouchers.Where(v => v.StoreId == null).ToList(); // chỉ voucher sàn
            

            // search
            if (!string.IsNullOrWhiteSpace(search))
            {
                vouchers = vouchers.Where(v =>
                    v.Reduce.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    v.MinOrder.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    v.Quantity.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // sort
            if (!string.IsNullOrWhiteSpace(sort))
            {
                vouchers = sort switch
                {
                    "newest" => vouchers.OrderByDescending(v => v.StartDate).ToList(),
                    "oldest" => vouchers.OrderBy(v => v.StartDate).ToList(),
                    "value-desc" => vouchers.OrderByDescending(v => v.Reduce).ToList(),
                    "value-asc" => vouchers.OrderBy(v => v.Reduce).ToList(),
                    _ => vouchers
                };
            }

            // pagination
            int totalItems = vouchers.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var pageData = vouchers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // user & saved (gọi API server→API)
            HashSet<int> savedIds = new();
            var uid = TryGetNumericUserId(User);
            if (uid is int userId)
            {
                var userVouchers = await _voucherService.GetUserVouchersAsync(userId);
                savedIds = userVouchers.Select(x => x.voucherId).ToHashSet();

                
            }
            if (HttpContext.Session.TryGetValue("Id", out var idBytes)
    && int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out var userIdd))
            {
                var store = await _storeService.GetStoreByUserIdAsync(userIdd);
                ViewData["StoreStatus"] = store?.Status; // enum StoreStatus
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.SavedIds = savedIds;

            // client không cần nhưng để 0 cũng không sao
            ViewBag.UserId = uid ?? 0;
            ViewBag.ApiBase = ""; // không dùng ở client nữa

            return View(pageData);
        }




        private int GetUserIdFromSession(HttpContext ctx)
        => int.TryParse(ctx.Session.GetString("Id"), out var id) ? id : 0;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] SaveVoucherBody body)
        {
            var userId = GetUserIdFromSession(HttpContext);
            if (userId <= 0) return Unauthorized(new { ok = false, message = "Bạn chưa đăng nhập." });

            if (body is null || body.VoucherId <= 0)
                return BadRequest(new { ok = false, message = "Thiếu VoucherId." });

            try
            {
                var result = await _voucherService.SaveAdminVoucherAsync(userId, body.VoucherId);
                return Json(new { ok = result.Ok, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = $"Lỗi MVC: {ex.Message}" });
            }
        }

        private static int? TryGetNumericUserId(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;
            var candidates = new[]
            {
                user.FindFirstValue("UserId"),               // claim số do bạn set khi login (khuyên dùng)
                user.FindFirstValue(ClaimTypes.NameIdentifier),
                user.FindFirstValue("sub"),
                user.FindFirstValue("uid"),
                user.FindFirstValue("user_id")
            };
            foreach (var s in candidates)
            {
                if (!string.IsNullOrWhiteSpace(s) && int.TryParse(s, out var id)) return id;
            }
            return null;
        }

        public class SaveVoucherBody { public int VoucherId { get; set; } }
    }
}
