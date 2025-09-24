using DATN_GO.Service;
using DATN_GO.Services;
using DATN_GO.ViewModels;
using DATN_GO.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Configuration;
using System.Text;
using System.Text.Json;

namespace DATN_GO.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly StoreService _storeService;

        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public CartController(CartService cartService, IConfiguration configuration, UserService userService, StoreService storeService)
        {
            _userService = userService;
            _storeService = storeService;
            _cartService = cartService;
            _configuration = configuration;

        }

        public async Task<IActionResult> Index()
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập để xem giỏ hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            var store = await _storeService.GetStoreByUserIdAsync(userId);
            ViewData["StoreStatus"] = store?.Status;

            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            var user = await _userService.GetUserByIdAsync(userId);
            ViewBag.Balance = user?.Balance ?? 0;

            var cartSummary = await _cartService.GetCartByUserIdAsync(userId)
                              ?? new CartSummaryViewModel();

            // 👉 nếu service giỏ hàng KHÔNG gán Addresses, tự gán rỗng hoặc nạp từ user/address service
            cartSummary.Addresses ??= new List<AddressViewModel>();

            // (nếu có API địa chỉ riêng, điền thật sự)
            // cartSummary.Addresses = await _userService.GetAddressesAsync(userId) ?? new List<AddressViewModel>();

            // voucher đã chọn
            var selectedVoucherId = HttpContext.Session.GetInt32("SelectedPlatformVoucherId") ?? 0;
            ViewBag.SelectedPlatformVoucherId = selectedVoucherId;

            ViewBag.UserId = userId;
            return View(cartSummary);
        }


        [HttpPut]
        [Route("cart/update-selection")]
        public async Task<IActionResult> UpdateSelection([FromBody] List<int> selectedCartIds)
        {
            if (!HttpContext.Session.TryGetValue("Id", out var idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out var userId))
            {
                return Unauthorized();
            }

            await _cartService.UpdateSelectionAsync(selectedCartIds ?? new List<int>());
            return Ok(new { message = "Selection updated" });
        }


        [HttpPost]
        public async Task<IActionResult> AddToCart(AddToCartRequest request)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            request.UserId = userId;

            var productVariantCount = ViewBag.VariantOptions is List<VariantWithValuesViewModel> variants
                ? variants.Count
                : 0;

            if (productVariantCount > 0 && (request.VariantValueIds == null || request.VariantValueIds.Count != productVariantCount))
            {
                TempData["ToastMessage"] = "Vui lòng chọn đầy đủ các biến thể.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("DetailProducts", "Products", new { id = request.ProductId });
            }

            var success = await _cartService.AddToCartAsync(request);
            if (success)
            {
                TempData["ToastMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index");
            }

            TempData["ToastMessage"] = "Thêm sản phẩm vào giỏ hàng thất bại.";
            TempData["ToastType"] = "danger";
            return RedirectToAction("DetailProducts", "Products", new { id = request.ProductId });
        }



        [HttpPost]
        public async Task<IActionResult> Remove(int cartId)
        {

            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                TempData["ToastMessage"] = "Bạn cần đăng nhập để thao tác.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            var success = await _cartService.RemoveFromCartAsync(cartId);


            if (success)
            {
                TempData["ToastMessage"] = "Đã sản phẩm khỏi giỏ hàng.";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Xóa sản phẩm thất bại.";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("Index");
        }


        [HttpPut]
        [HttpPost] // Support both PUT và POST
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                // Validate input
                if (request.NewQuantity <= 0)
                {
                    return BadRequest(new { message = "Số lượng phải lớn hơn 0" });
                }

                var success = await _cartService.UpdateQuantityAsync(request.CartId, request.NewQuantity);

                if (success)
                {
                    return Ok(new { message = "Cập nhật thành công" });
                }
                else
                {
                    return BadRequest(new { message = "Cập nhật số lượng thất bại" });
                }
            }
            catch (Exception ex)
            {
                // Log error here if you have logging
                return StatusCode(500, new { message = "Có lỗi xảy ra", error = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateVoucherDropdown([FromBody] List<int> selectedCartIds)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                return Unauthorized();
            }

            await _cartService.UpdateSelectionAsync(selectedCartIds);

            var cartSummary = await _cartService.GetCartByUserIdAsync(userId);
            if (cartSummary == null) return BadRequest();

            // 👉 lấy lại id voucher đang chọn từ Session để Razor đánh dấu selected
            var selectedVoucherId = HttpContext.Session.GetInt32("SelectedPlatformVoucherId") ?? 0;
            ViewBag.SelectedPlatformVoucherId = selectedVoucherId;

            return PartialView("_VoucherDropdown", cartSummary.Vouchers);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateStoreVoucherOptions([FromBody] StoreVoucherRequest req)
        {
            if (!HttpContext.Session.TryGetValue("Id", out var idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out var userId))
                return Unauthorized();

            // cập nhật selection trước để service tính lại voucher hợp lệ
            await _cartService.UpdateSelectionAsync(req?.SelectedCartIds ?? new List<int>());

            var cartSummary = await _cartService.GetCartByUserIdAsync(userId);
            if (cartSummary == null) return BadRequest();

            // LẤY VOUCHER THEO SHOP + KHỬ TRÙNG LẶP
            var storeVouchers = cartSummary.Vouchers
                .Where(v => v.StoreId == req.StoreId)
                .GroupBy(v => v.VoucherId)
                .Select(g => g.First())
                .OrderByDescending(v => v.IsPercentage)
                .ThenByDescending(v => v.Reduce)
                .ToList();

            return PartialView("_StoreVoucherOptions", storeVouchers);
        }



        [HttpPost]
        public IActionResult SaveSelectedPlatformVoucher([FromBody] int voucherId)
        {
            HttpContext.Session.SetInt32("SelectedPlatformVoucherId", voucherId);
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> GetShippingFee([FromBody] ShippingGroupRequest request)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                return Unauthorized();
            }

            var shippingGroups = await _cartService.GetShippingGroupsAsync(userId, request.AddressId);

            if (shippingGroups == null)
                return BadRequest();

            return Json(shippingGroups);
        }



        // HIỂN THỊ CART
        private int GetUserIdOr0()
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes)) return 0;
            return int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId) ? userId : 0;
        }

        private static int CalcCartCount(CartSummaryViewModel? summary)
        {
            // Nếu muốn đếm số cart item thay vì quantity
            if (summary?.CartItems != null && summary.CartItems.Count > 0)
                return summary.CartItems.Count;

            return 0;
        }


        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = GetUserIdOr0();
            if (userId == 0)
            {
                HttpContext.Session.SetInt32("CartCount", 0);
                return Json(new { count = 0 });
            }

            int total = 0;
            try
            {
                var summary = await _cartService.GetCartByUserIdAsync(userId);
                total = CalcCartCount(summary); // <-- đổi hàm
            }
            catch
            {
                // Lỗi API hoặc network => cứ trả 0 cho an toàn
                total = 0;
            }

            HttpContext.Session.SetInt32("CartCount", total);

            return Json(new { count = total });
        }



        // THÊM PRODUCT NULL VARIANT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdd([FromForm] int productId, [FromForm] int quantity = 1)
        {
            // dùng helper cũ của bạn (nếu chưa có thì thêm y như trước)
            int userId = 0;
            if (!HttpContext.Session.TryGetValue("Id", out var idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out userId))
            {
                // không login → count = 0
                HttpContext.Session.SetInt32("CartCount", 0);
                return Json(new { success = false, message = "Bạn cần đăng nhập.", cartCount = 0 });
            }

            var req = new AddToCartRequest
            {
                UserId = userId,
                ProductId = productId,
                Quantity = quantity,
                VariantValueIds = new List<int>() // không biến thể
            };

            var ok = await _cartService.AddToCartAsync(req);

            // tính lại tổng item để trả về/ghi session
            var sum = await _cartService.GetCartByUserIdAsync(userId);
            var total = (sum?.CartItems != null) ? sum.CartItems.Sum(i => i.Quantity) : 0;
            HttpContext.Session.SetInt32("CartCount", total);

            return Json(new
            {
                success = ok,
                message = ok ? "Đã thêm vào giỏ hàng." : "Thêm sản phẩm vào giỏ hàng thất bại.",
                cartCount = total
            });
        }




    }
}