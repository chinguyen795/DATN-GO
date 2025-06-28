using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoresController : ControllerBase
    {
        private readonly IStoresService _service;

        public StoresController(IStoresService service)
        {
            _service = service;
        }

        // GET: api/stores
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var stores = await _service.GetAllAsync();
            return Ok(stores);
        }

        // GET: api/stores/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var store = await _service.GetByIdAsync(id);
            if (store == null) return NotFound();
            return Ok(store);
        }

        // POST: api/stores
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Stores model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/stores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Stores model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy store");
            return NoContent();
        }

        // DELETE: api/stores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}