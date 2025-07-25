using DATN_GO.Service;
using DATN_GO.ViewModels.Cart;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DATN_GO.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
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

            var cartItems = await _cartService.GetCartByUserIdAsync(userId);
            return View(cartItems);
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

            var success = await _cartService.AddToCartAsync(request);
            if (success)
                return RedirectToAction("Index");

            TempData["Error"] = "Thêm sản phẩm vào giỏ hàng thất bại";
            return RedirectToAction("Index", "Products");
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




    }
}
