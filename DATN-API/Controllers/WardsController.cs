using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WardsController : ControllerBase
    {
        private readonly IWardsService _service;

        public WardsController(IWardsService service)
        {
            _service = service;
        }

        // GET: api/wards
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var wards = await _service.GetAllAsync();
            return Ok(wards);
        }

        // GET: api/wards/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ward = await _service.GetByIdAsync(id);
            if (ward == null) return NotFound();
            return Ok(ward);
        }

        // GET: api/wards/district/3
        [HttpGet("district/{districtId}")]
        public async Task<IActionResult> GetByDistrictId(int districtId)
        {
            var wards = await _service.GetByDistrictIdAsync(districtId);
            return Ok(wards);
        }

        // POST: api/wards

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] Wards model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    model.District = null;

        //    var created = await _service.CreateAsync(model);
        //    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        //}


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Wards model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var created = await _service.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/wards/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Wards model)
        {
            if (!await _service.UpdateAsync(id, model))
                return BadRequest("ID không khớp hoặc không tìm thấy ward");
            return NoContent();
        }

        // DELETE: api/wards/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.DeleteAsync(id))
                return NotFound();
            return NoContent();
        }
    }
}