using DATN_GO.Models;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static DATN_GO.Services.OcrService;
using System.Threading.Tasks;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DATN_GO.Controllers
{
    public class SalesRegistrationController : Controller
    {
        
        private readonly OcrService _ocrService;
        private readonly GoogleCloudStorageService _gcsService;
        public SalesRegistrationController(OcrService ocrService, GoogleCloudStorageService gcsService)
        {
            _ocrService = ocrService;
            _gcsService = gcsService;
        }

        [HttpGet]
        public IActionResult SalesRegistration()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SalesRegistration(SalesRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Lấy UserId từ user đăng nhập
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type.EndsWith("/nameidentifier"));
            if (userIdClaim == null)
            {
                ModelState.AddModelError("", "Không xác định được người dùng.");
                return View(model);
            }
            int userId = int.Parse(userIdClaim.Value);

            // Upload avatar và cover photo lên GCS
            string? avatarUrl = null;
            string? coverUrl = null;
            if (model.Avatar != null)
                avatarUrl = await _gcsService.UploadFileAsync(model.Avatar, "avatars");
            if (model.CoverPhoto != null)
                coverUrl = await _gcsService.UploadFileAsync(model.CoverPhoto, "covers");

            // Chuẩn bị dữ liệu gửi API
            var saveInfoRequest = new {
                UserId = userId,
                CitizenIdentityCard = model.CitizenIdentityCard,
                RepresentativeName = model.RepresentativeName,
                Address = model.Address,
                BankAccount = model.BankAccount,
                Bank = model.Bank,
                Avatar = avatarUrl,
                CoverPhoto = coverUrl
            };
            var result = await _ocrService.SaveInfoFromOcrAsync(saveInfoRequest);
            if (result)
            {
                TempData["Success"] = "Đăng ký bán hàng thành công. Vui lòng chờ duyệt.";
                return RedirectToAction("SalesRegistration");
            }
            else
            {
                ModelState.AddModelError("", "Đăng ký thất bại. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExtractOcr([FromForm] OcrRequest request)
        {
            if (request.ImageFile == null || request.ImageFile.Length == 0)
                return Json(new { success = false, message = "Vui lòng chọn file ảnh." });
            var result = await _ocrService.ExtractFromImageAsync(request.ImageFile);
            if (result == null)
                return Json(new { success = false, message = "Không nhận diện được thông tin." });
            return Json(new { success = true, data = result });
        }
    }
}
