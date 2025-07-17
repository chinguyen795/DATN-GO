using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DATN_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OcrController : ControllerBase
    {
        private readonly IOcrService _ocrService;

        public OcrController(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        [HttpPost("cccd")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExtractFromImage([FromForm] OcrRequest request)
        {
            if (request.ImageFile == null || request.ImageFile.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file ảnh." });

            var result = await _ocrService.ExtractFromImageAsync(request.ImageFile);
            return Content(result, "application/json");
        }


        [HttpPost("save-info")]
        public IActionResult SaveInfoFromOcr([FromBody] Models.OcrSaveInfoRequest request)
        {
            return _ocrService.SaveInfoFromOcr(request);
        }
    }
}
