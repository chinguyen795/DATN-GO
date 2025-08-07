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
        private readonly IConfiguration _configuration;

        public CartController(CartService cartService, IConfiguration configuration)
        {
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
            ViewBag.ApiBaseUrl = _configuration["ApiSettings:BaseUrl"];

            var cartSummary = await _cartService.GetCartByUserIdAsync(userId);
            ViewBag.UserId = userId;
            return View(cartSummary);

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



        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(UpdateQuantityRequest request)
        {
            var success = await _cartService.UpdateQuantityAsync(request.CartId, request.NewQuantity);

            if (!success)
            {
                TempData["Error"] = "Cập nhật số lượng thất bại.";
            }

            return RedirectToAction("Index");
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
            if (cartSummary == null)
            {
                return BadRequest();
            }

            return PartialView("_VoucherDropdown", cartSummary.Vouchers);
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



    }
}
