using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/carts
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var carts = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.Product)
                .ToListAsync();

            return Ok(carts);
        }

        // GET: api/carts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cart = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cart == null) return NotFound();

            return Ok(cart);
        }

        // GET: api/carts/user/3
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var userCarts = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Ok(userCarts);
        }

        // POST: api/carts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Carts model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.CreateAt = DateTime.UtcNow;
            _context.Carts.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/carts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Carts model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var cart = await _context.Carts.FindAsync(id);
            if (cart == null) return NotFound();

            cart.ProductId = model.ProductId;
            cart.Quantity = model.Quantity;
            cart.CreateAt = model.CreateAt;
            cart.UserId = model.UserId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/carts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null) return NotFound();

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
