using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingMethodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShippingMethodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/shippingmethods
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var methods = await _context.ShippingMethods
                .Include(sm => sm.Diner)
                .Include(sm => sm.Orders)
                .ToListAsync();

            return Ok(methods);
        }

        // GET: api/shippingmethods/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var method = await _context.ShippingMethods
                .Include(sm => sm.Diner)
                .Include(sm => sm.Orders)
                .FirstOrDefaultAsync(sm => sm.Id == id);

            if (method == null) return NotFound();

            return Ok(method);
        }

        // GET: api/shippingmethods/diner/3
        [HttpGet("diner/{dinerId}")]
        public async Task<IActionResult> GetByDinerId(int dinerId)
        {
            var methods = await _context.ShippingMethods
                .Where(sm => sm.DinerId == dinerId)
                .Include(sm => sm.Orders)
                .ToListAsync();

            return Ok(methods);
        }

        // POST: api/shippingmethods
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ShippingMethods model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.ShippingMethods.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/shippingmethods/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShippingMethods model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var method = await _context.ShippingMethods.FindAsync(id);
            if (method == null) return NotFound();

            method.DinerId = model.DinerId;
            method.Price = model.Price;
            method.MethodName = model.MethodName;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/shippingmethods/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var method = await _context.ShippingMethods.FindAsync(id);
            if (method == null) return NotFound();

            _context.ShippingMethods.Remove(method);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
