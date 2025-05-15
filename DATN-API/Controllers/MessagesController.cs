using DATN_API.Data;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/messages
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .ToListAsync();

            return Ok(messages);
        }

        // GET: api/messages/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null) return NotFound();

            return Ok(message);
        }

        // GET: api/messages/conversation/3/7
        [HttpGet("conversation/{user1Id}/{user2Id}")]
        public async Task<IActionResult> GetConversation(int user1Id, int user2Id)
        {
            var conversation = await _context.Messages
                .Where(m =>
                    (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                    (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(conversation);
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Messages model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.Timestamp = DateTime.UtcNow;

            _context.Messages.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/messages/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Messages model)
        {
            if (id != model.Id)
                return BadRequest("ID không khớp");

            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            message.SenderId = model.SenderId;
            message.ReceiverId = model.ReceiverId;
            message.Text = model.Text;
            message.Timestamp = model.Timestamp;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/messages/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
