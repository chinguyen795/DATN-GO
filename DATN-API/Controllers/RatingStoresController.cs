using DATN_API.Models;
using DATN_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingStoresController : ControllerBase
    {
        private readonly IRatingStoresService _service;

        public RatingStoresController(IRatingStoresService service)
        {
            _service = service;
        }

        // GET: api/ratingstores
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var ratings = await _service.GetAllAsync();
            return Ok(ratings);
        }

        // GET: api/ratingstores/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rating = await _service.GetByIdAsync(id);
            if (rating == null) return NotFound();
            return Ok(rating);
        }

        // POST: api/ratingstores
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RatingStores model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/ratingstores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RatingStores model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy rating");
            return NoContent();
        }

        // DELETE: api/ratingstores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}
