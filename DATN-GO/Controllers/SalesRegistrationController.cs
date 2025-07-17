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
        private readonly BankService _bankService;
        private readonly OcrService _ocrService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly ILogger<SalesRegistrationController> _logger;

        public SalesRegistrationController(
            OcrService ocrService,
            GoogleCloudStorageService gcsService,
            BankService bankService,
            ILogger<SalesRegistrationController> logger)
        {
            _ocrService = ocrService;
            _gcsService = gcsService;
            _bankService = bankService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Pendingapproval()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SalesRegistration()
        {
            var model = new SalesRegistrationViewModel();
            ViewBag.Banks = await _bankService.GetBankListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalesRegistration(SalesRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(" ModelState không hợp lệ:");
                foreach (var kvp in ModelState)
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        _logger.LogWarning($"{kvp.Key}: {error.ErrorMessage}");
                    }
                }
                ViewBag.Banks = await _bankService.GetBankListAsync();
                return View(model);
            }

            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                _logger.LogError(" Không tìm thấy Id trong session.");
                TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                TempData["ToastType"] = "danger";
                ViewBag.Banks = await _bankService.GetBankListAsync();
                return View(model);
            }


            var avatarFile = Request.Form.Files["Avatar"];
            var coverFile = Request.Form.Files["CoverPhoto"];

            if (avatarFile != null && avatarFile.Length > 0)
                model.Avatar = await _gcsService.UploadFileAsync(avatarFile, "seller/avatars/");
            _logger.LogInformation($" Avatar uploaded: {model.Avatar}");

            if (coverFile != null && coverFile.Length > 0)
                model.CoverPhoto = await _gcsService.UploadFileAsync(coverFile, "seller/covers/");
            _logger.LogInformation($" Cover uploaded: {model.CoverPhoto}");

            var saveInfoRequest = new
            {
                UserId = userId,
                CitizenIdentityCard = model.CitizenIdentityCard,
                RepresentativeName = model.RepresentativeName,
                Address = model.Address,
                AvatarUrl = model.Avatar, 
                CoverUrl = model.CoverPhoto,
                Name = model.Name,
                BankAccount = model.BankAccount,
                Bank = model.Bank,
                BankAccountOwner = model.BankAccountOwner
            };

            _logger.LogInformation(" Gửi request SaveInfoFromOcrAsync:");
            _logger.LogInformation(JsonSerializer.Serialize(saveInfoRequest));

            var result = await _ocrService.SaveInfoFromOcrAsync(saveInfoRequest);
            if (result)
            {
                TempData["Success"] = "Đăng ký bán hàng thành công.";
                return RedirectToAction("SalesRegistration");
            }

            _logger.LogError(" Gọi API SaveInfoFromOcrAsync thất bại.");
            ViewBag.Banks = await _bankService.GetBankListAsync();
            ModelState.AddModelError("", "Đăng ký thất bại.");
            return View(model);
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
