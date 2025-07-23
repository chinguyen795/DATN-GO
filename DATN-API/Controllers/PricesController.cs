using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PricesController : ControllerBase
    {
        private readonly IPricesService _pricesService;
        public PricesController(IPricesService pricesService)
        {
            _pricesService = pricesService;
        }

        // GET: api/Prices
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _pricesService.GetAllAsync();
            return Ok(result);
        }

        // GET: api/Prices/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var price = await _pricesService.GetByIdAsync(id);
            if (price == null)
                return NotFound();

            return Ok(price);
        }

        // POST: api/Prices
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Prices model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _pricesService.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/Prices/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Prices model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _pricesService.UpdateAsync(id, model);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE: api/Prices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _pricesService.DeleteAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpGet("min-price/{productId}")]
        public async Task<IActionResult> GetMinPrice(int productId)
        {
            var minPrice = await _pricesService.GetMinPriceByProductIdAsync(productId);
            if (minPrice == null)
                return NotFound();

            return Ok(minPrice);
        }

        [HttpGet("min-max-price/{productId}")]
        public async Task<IActionResult> GetMinMaxPrice(int productId)
        {
            var result = await _pricesService.GetMinMaxPriceByProductIdAsync(productId);
            return Ok(result);
        }


    }
}
