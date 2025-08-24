using DATN_API.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly IAiChatService _ai;

        public AiController(IAiChatService ai) => _ai = ai;

        public class ChatRequest { public string Message { get; set; } = ""; }
        public class ChatResponse { public string Answer { get; set; } = ""; }

        // Cho phép MVC call không cần token (nếu bạn muốn bắt buộc đăng nhập, bỏ AllowAnonymous)
        [HttpPost("chat")]
        [AllowAnonymous]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Message))
                return BadRequest(new { error = "Message rỗng" });

            var answer = await _ai.AskAsync(req.Message);
            return Ok(new ChatResponse { Answer = answer });
        }
    }
}