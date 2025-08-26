using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DecoratesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DecoratesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/decorates
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var decorates = await _context.Decorates
                .Include(d => d.User)
                .ToListAsync();
            return Ok(decorates);
        }

        // GET: api/decorates/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var decorate = await _context.Decorates
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (decorate == null) return NotFound();

            return Ok(decorate);
        }

        // POST: api/decorates
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Decorates model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ❌ Bỏ check UserId, chỉ tạo decorate mới
            _context.Decorates.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }


        // PUT: api/decorates/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Decorates model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null)
                return NotFound();

            // ✅ Chỉ cập nhật nếu có dữ liệu (bỏ UserId)
            if (!string.IsNullOrEmpty(model.Video)) decorate.Video = model.Video;

            // 🎯 Cập nhật slide
            if (!string.IsNullOrEmpty(model.Slide1)) decorate.Slide1 = model.Slide1;
            if (!string.IsNullOrEmpty(model.TitleSlide1)) decorate.TitleSlide1 = model.TitleSlide1;
            if (!string.IsNullOrEmpty(model.DescriptionSlide1)) decorate.DescriptionSlide1 = model.DescriptionSlide1;

            if (!string.IsNullOrEmpty(model.Slide2)) decorate.Slide2 = model.Slide2;
            if (!string.IsNullOrEmpty(model.TitleSlide2)) decorate.TitleSlide2 = model.TitleSlide2;
            if (!string.IsNullOrEmpty(model.DescriptionSlide2)) decorate.DescriptionSlide2 = model.DescriptionSlide2;

            if (!string.IsNullOrEmpty(model.Slide3)) decorate.Slide3 = model.Slide3;
            if (!string.IsNullOrEmpty(model.TitleSlide3)) decorate.TitleSlide3 = model.TitleSlide3;
            if (!string.IsNullOrEmpty(model.DescriptionSlide3)) decorate.DescriptionSlide3 = model.DescriptionSlide3;

            if (!string.IsNullOrEmpty(model.Slide4)) decorate.Slide4 = model.Slide4;
            if (!string.IsNullOrEmpty(model.TitleSlide4)) decorate.TitleSlide4 = model.TitleSlide4;
            if (!string.IsNullOrEmpty(model.DescriptionSlide4)) decorate.DescriptionSlide4 = model.DescriptionSlide4;

            if (!string.IsNullOrEmpty(model.Slide5)) decorate.Slide5 = model.Slide5;
            if (!string.IsNullOrEmpty(model.TitleSlide5)) decorate.TitleSlide5 = model.TitleSlide5;
            if (!string.IsNullOrEmpty(model.DescriptionSlide5)) decorate.DescriptionSlide5 = model.DescriptionSlide5;

            // 🎯 Cập nhật ảnh decorate 1
            if (!string.IsNullOrEmpty(model.Image1)) decorate.Image1 = model.Image1;
            if (!string.IsNullOrEmpty(model.Title1)) decorate.Title1 = model.Title1;
            if (!string.IsNullOrEmpty(model.Description1)) decorate.Description1 = model.Description1;

            // 🎯 Cập nhật ảnh decorate 2
            if (!string.IsNullOrEmpty(model.Image2)) decorate.Image2 = model.Image2;
            if (!string.IsNullOrEmpty(model.Title2)) decorate.Title2 = model.Title2;
            if (!string.IsNullOrEmpty(model.Description2)) decorate.Description2 = model.Description2;

            await _context.SaveChangesAsync();

            return NoContent();
        }


        // DELETE: api/decorates/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return NotFound();

            _context.Decorates.Remove(decorate);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/decorates/user/{userId} (legacy, nhưng trả global)
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var decorate = await _context.Decorates.FirstOrDefaultAsync();

            if (decorate == null)
                return NotFound("Không tìm thấy decorate global!");

            return Ok(decorate);
        }

        // HÀM HỖ TRỢ XÓA TỪNG MỤC

        // XÓA SLIDES
        [HttpPatch("{id}/clear-slides")]
        public async Task<IActionResult> ClearSlides(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null)
                return NotFound("Không tìm thấy trang trí");

            decorate.Slide1 = decorate.TitleSlide1 = decorate.DescriptionSlide1 = null;
            decorate.Slide2 = decorate.TitleSlide2 = decorate.DescriptionSlide2 = null;
            decorate.Slide3 = decorate.TitleSlide3 = decorate.DescriptionSlide3 = null;
            decorate.Slide4 = decorate.TitleSlide4 = decorate.DescriptionSlide4 = null;
            decorate.Slide5 = decorate.TitleSlide5 = decorate.DescriptionSlide5 = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // XÓA VIDEO
        [HttpPatch("{id}/clear-video")]
        public async Task<IActionResult> ClearVideo(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null)
                return NotFound("Không tìm thấy trang trí");

            decorate.Video = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }


        // XÓA ẢNH 1
        [HttpPatch("{id}/clear-decorate1")]
        public async Task<IActionResult> ClearDecorate1(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null)
                return NotFound("Không tìm thấy trang trí");

            decorate.Image1 = decorate.Title1 = decorate.Description1 = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // XÓA ẢNH 2
        [HttpPatch("{id}/clear-decorate2")]
        public async Task<IActionResult> ClearDecorate2(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null)
                return NotFound("Không tìm thấy trang trí");

            decorate.Image2 = decorate.Title2 = decorate.Description2 = null;

            await _context.SaveChangesAsync();
            return NoContent();
        }



    }
}