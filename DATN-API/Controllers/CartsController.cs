using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.Services;
using DATN_API.ViewModels.Cart;
using DATN_API.ViewModels.GHTK;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var result = await _cartService.AddToCartAsync(request);
            if (result) return Ok(new { message = "Thêm vào giỏ hàng thành công" });
            return BadRequest(new { message = "Thêm giỏ hàng thất bại" });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCartByUserId(int userId)
        {
            var cart = await _cartService.GetCartByUserIdAsync(userId);
            return Ok(cart);
        }

        [HttpDelete("remove/{cartId}")]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var result = await _cartService.RemoveFromCartAsync(cartId);
            if (result) return Ok(new { message = "Xóa sản phẩm khỏi giỏ hàng thành công" });
            return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng" });
        }

        [HttpPut("update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var result = await _cartService.UpdateQuantityAsync(request.CartId, request.NewQuantity);
            if (result) return Ok(new { message = "Cập nhật số lượng thành công" });
            return BadRequest(new { message = "Cập nhật số lượng thất bại" });
        }

        [HttpPut("update-selection")]
        public async Task<IActionResult> UpdateSelection([FromBody] List<int> selectedCartIds)
        {
            await _cartService.UpdateSelectionAsync(selectedCartIds);
            return Ok();
        }

        [HttpGet("selected-vouchers")]
        public async Task<IActionResult> GetSelectedVouchers(int userId)
        {
            var cartSummary = await _cartService.GetCartByUserIdAsync(userId);
            if (cartSummary == null)
                return NotFound();

            return Ok(cartSummary.Vouchers);
        }

        [HttpPost("shipping-groups")]
        public async Task<IActionResult> GetShippingGroups([FromBody] ShippingGroupRequest request)
        {
            var result = await _cartService.GetShippingGroupsByUserIdAsync(request.UserId, request.AddressId);
            return Ok(result);
        }

        [HttpPost("create-ghtk-order")]
        public async Task<IActionResult> CreateGhtkOrder(int userId, int addressId)
        {
            var result = await _cartService.CreateGHTKOrderAsync(userId, addressId);
            return Ok(result);
        }

        [HttpPost("cancel-ghtk-order")]
        public async Task<IActionResult> CancelGhtkOrder(string orderCode, int userId)
        {
            var result = await _cartService.CancelGHTKOrderAsync(orderCode, userId);

            if (result)
                return Ok(new { message = $"Hủy đơn hàng {orderCode} thành công" });

            return BadRequest(new { message = $"Hủy đơn hàng {orderCode} thất bại" });
        }

        [HttpGet("ghtk-order-status")]
        public async Task<IActionResult> GetGhtkOrderStatus(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
                return BadRequest(new { message = "Mã đơn hàng không hợp lệ" });

            var status = await _cartService.GetGHTKOrderStatusAsync(orderCode);
            return Ok(status);
        }


    }

}
