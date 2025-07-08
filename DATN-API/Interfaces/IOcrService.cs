using Microsoft.AspNetCore.Mvc;
using DATN_API.Models;

namespace DATN_API.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractFromImageAsync(IFormFile imageFile);
        IActionResult SaveInfoFromOcr(OcrSaveInfoRequest request);
    }
}
