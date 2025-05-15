using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/reviews
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Include(r => r.Order)
                .ToListAsync();

            return Ok(reviews);
        }

        // GET: api/reviews/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null) return NotFound();

            return Ok(review);
        }

        // GET: api/reviews/product/3
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .Include(r => r.Order)
                .ToListAsync();

            return Ok(reviews);
        }

        // GET: api/reviews/user/4
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Product)
                .Include(r => r.Order)
                .ToListAsync();

            return Ok(reviews);
        }

        // POST: api/reviews
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Reviews model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreateAt = DateTime.UtcNow;

            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/reviews/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Reviews model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.UserId = model.UserId;
            review.ProductId = model.ProductId;
            review.OrderId = model.OrderId;
            review.Rating = model.Rating;
            review.CommentText = model.CommentText;
            review.CreateAt = model.CreateAt;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/reviews/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
