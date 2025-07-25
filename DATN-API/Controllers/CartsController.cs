using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.Services;
using DATN_API.ViewModels.Cart;
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


    }

}
