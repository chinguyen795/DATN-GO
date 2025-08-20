using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewsService _reviewsService;

        public ReviewsController(IReviewsService reviewsService)
        {
            _reviewsService = reviewsService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddReview([FromBody] ReviewCreateRequest request)
        {
            try
            {
                var review = new Reviews
                {
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    OrderId = request.OrderId,
                    Rating = request.Rating,
                    CommentText = request.CommentText
                };

                var result = await _reviewsService.CreateAsync(review, request.MediaList);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _reviewsService.GetAllAsync();
            return Ok(reviews);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewById(int id)
        {
            var review = await _reviewsService.GetByIdAsync(id);
            if (review == null) return NotFound();
            return Ok(review);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var result = await _reviewsService.DeleteAsync(id);
            if (!result) return NotFound(new { message = "Không tìm thấy đánh giá" });

            return Ok(new { message = "Xóa đánh giá thành công" });
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetReviewsByProductId(int productId)
        {
            var reviews = await _reviewsService.GetByProductIdAsync(productId);
            return Ok(reviews);
        }

        [HttpGet("has-review/{orderId}/product/{productId}/user/{userId}")]
        public async Task<IActionResult> HasReview(int orderId, int productId, int userId)
        {
            var hasReview = await _reviewsService.HasUserReviewedProductAsync(orderId, productId, userId);
            return Ok(hasReview);
        }



        [HttpGet("is-order-completed/{orderId}")]
        public async Task<IActionResult> IsOrderCompleted(int orderId)
        {
            var completed = await _reviewsService.IsOrderCompletedAsync(orderId);
            return Ok(completed);
        }

        [HttpGet("completed-orders/{userId}")]
        public async Task<IActionResult> GetCompletedOrdersByUser(int userId)
        {
            var orders = await _reviewsService.GetCompletedOrdersByUserAsync(userId);
            return Ok(orders);
        }


    }

}