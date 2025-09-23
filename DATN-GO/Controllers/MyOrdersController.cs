using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DATN_GO.Service;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;

public class MyOrdersController : Controller
{
    private readonly OrderService _orders;
    private readonly StoreService _storeService;

    public MyOrdersController(OrderService orders, StoreService storeService)
    {
        _orders = orders;
        _storeService = storeService;
    }


    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = TryGetUserIdFromClaims()
                  ?? TryGetUserIdFromSession()
                  ?? TryGetUserIdFromJwtCookie();   // đọc thêm từ cookie JWT nếu có

        if (userId is null)
        {
            TempData["ToastMessage"] = "Vui lòng đăng nhập để xem đơn hàng.";
            TempData["ToastType"] = "danger";
            return RedirectToAction("Login", "UserAuthentication");
        }

        if (userId is int uid)
        {
            var store = await _storeService.GetStoreByUserIdAsync(uid);
            if (store != null)
                ViewData["StoreStatus"] = store.Status; // enum StoreStatus
        }

        var (ok, data, msg) = await _orders.GetOrdersByUserAsync(userId.Value);
        if (!ok)
        {
            TempData["ToastMessage"] = msg ?? "Không tải được danh sách đơn hàng.";
            TempData["ToastType"] = "danger";
            data = new List<OrderViewModel>();
        }
        return View(data);
    }

    private int? TryGetUserIdFromSession()
    {
        var s = HttpContext.Session.GetString("UserId"); // <-- dùng "UserId"
        return int.TryParse(s, out var id) ? id : (int?)null;
    }

    private int? TryGetUserIdFromClaims()
    {
        var c = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("UserId");
        return int.TryParse(c?.Value, out var id) ? id : (int?)null;
    }

    private int? TryGetUserIdFromJwtCookie()
    {
        // đoán tên cookie; đổi nếu bạn biết chính xác
        var names = new[] { "jwt", "access_token", "AuthToken" };
        string? token = names.Select(n => Request.Cookies.TryGetValue(n, out var v) ? v : null)
                             .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var cid = jwt.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" ||
                c.Type == "sub" || c.Type == "uid" || c.Type == "userId");

            return int.TryParse(cid?.Value, out var id) ? id : (int?)null;
        }
        catch { return null; }
    }
}
