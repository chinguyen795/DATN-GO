using DATN_GO.Models;
using DATN_GO.Service;
using DATN_GO.ViewModels.Decorates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DATN_GO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DecoratesController : Controller
    {

        private readonly DecoratesService _decorationService;
        private readonly GoogleCloudStorageService _googleCloudStorageService;

        public DecoratesController(DecoratesService decorationService, GoogleCloudStorageService googleCloudStorageService)
        {
            _decorationService = decorationService;
            _googleCloudStorageService = googleCloudStorageService;
        }

        // Index
        public async Task<IActionResult> Decorates()
        {
            // vẫn yêu cầu đăng nhập
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // vẫn khóa admin
            var user = await _decorationService.GetUserByIdAsync(userId);
            if (user == null || user.RoleId != 3)
            {
                TempData["ToastMessage"] = "Bạn không có quyền truy cập vào trang này!";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // ⬇️ Lấy decorate GLOBAL (thay cho GetDecorateByUserIdAsync)
            var decorate = await _decorationService.GetGlobalDecorateAsync();

            var decorateViewModel = new DecoratesViewModel
            {
                Id = decorate?.Id ?? 0,

                // KHÔNG còn UserId / AdminSettingId
                TitleSlide1 = decorate?.TitleSlide1 ?? "",
                DescriptionSlide1 = decorate?.DescriptionSlide1 ?? "",
                TitleSlide2 = decorate?.TitleSlide2 ?? "",
                DescriptionSlide2 = decorate?.DescriptionSlide2 ?? "",
                TitleSlide3 = decorate?.TitleSlide3 ?? "",
                DescriptionSlide3 = decorate?.DescriptionSlide3 ?? "",
                TitleSlide4 = decorate?.TitleSlide4 ?? "",
                DescriptionSlide4 = decorate?.DescriptionSlide4 ?? "",
                TitleSlide5 = decorate?.TitleSlide5 ?? "",
                DescriptionSlide5 = decorate?.DescriptionSlide5 ?? "",

                Slide1Path = decorate?.Slide1 ?? "",
                Slide2Path = decorate?.Slide2 ?? "",
                Slide3Path = decorate?.Slide3 ?? "",
                Slide4Path = decorate?.Slide4 ?? "",
                Slide5Path = decorate?.Slide5 ?? "",

                Image1Path = decorate?.Image1 ?? "",
                Image2Path = decorate?.Image2 ?? "",
                VideoPath = decorate?.Video ?? "",

                Title1 = decorate?.Title1 ?? "",
                Title2 = decorate?.Title2 ?? "",
                Description1 = decorate?.Description1 ?? "",
                Description2 = decorate?.Description2 ?? ""
            };

            ViewBag.UserInfo = user; // vẫn hiển thị info user ở header
            return View(decorateViewModel);
        }


        // Tạo decorate 
        [HttpPost]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> Create([FromForm] DecoratesViewModel model)
        {
            try
            {
                // Check login
                var userIdStr = HttpContext.Session.GetString("Id");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    TempData["CustomToastType"] = "error";
                    TempData["CustomToastMessage"] = "Bạn chưa đăng nhập!";
                    return Json(new
                    {
                        success = false,
                        redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                    });
                }

                // Validate
                var anySlide = new[] { model.Slide1, model.Slide2, model.Slide3, model.Slide4, model.Slide5 }.Any(x => x != null);
                var anyDecorImg = model.Image1 != null || model.Image2 != null;
                var anyText = !(string.IsNullOrWhiteSpace(model.Title1) && string.IsNullOrWhiteSpace(model.Title2)
                    && string.IsNullOrWhiteSpace(model.Description1) && string.IsNullOrWhiteSpace(model.Description2));
                var hasVideo = model.Video != null;

                if (!anySlide && !anyDecorImg && !anyText && !hasVideo)
                {
                    TempData["CustomToastType"] = "error";
                    TempData["CustomToastMessage"] = "Vui lòng nhập đầy đủ thông tin trước khi tạo trang trí!";
                    return Json(new
                    {
                        success = false,
                        redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                    });
                }

                // Save files
                string? slide1Path = model.Slide1 != null ? await SaveFileAsync(model.Slide1, "decorates/slideshow") : null;
                string? slide2Path = model.Slide2 != null ? await SaveFileAsync(model.Slide2, "decorates/slideshow") : null;
                string? slide3Path = model.Slide3 != null ? await SaveFileAsync(model.Slide3, "decorates/slideshow") : null;
                string? slide4Path = model.Slide4 != null ? await SaveFileAsync(model.Slide4, "decorates/slideshow") : null;
                string? slide5Path = model.Slide5 != null ? await SaveFileAsync(model.Slide5, "decorates/slideshow") : null;

                string? image1Path = model.Image1 != null ? await SaveFileAsync(model.Image1, "decorates/images") : null;
                string? image2Path = model.Image2 != null ? await SaveFileAsync(model.Image2, "decorates/images") : null;

                string? videoPath = model.Video != null ? await SaveFileAsync(model.Video, "decorates/videos") : null;

                var decorate = new Decorates
                {
                    TitleSlide1 = model.Slide1 != null ? model.TitleSlide1 ?? "" : "",
                    DescriptionSlide1 = model.Slide1 != null ? model.DescriptionSlide1 ?? "" : "",

                    TitleSlide2 = model.Slide2 != null ? model.TitleSlide2 ?? "" : "",
                    DescriptionSlide2 = model.Slide2 != null ? model.DescriptionSlide2 ?? "" : "",

                    TitleSlide3 = model.Slide3 != null ? model.TitleSlide3 ?? "" : "",
                    DescriptionSlide3 = model.Slide3 != null ? model.DescriptionSlide3 ?? "" : "",

                    TitleSlide4 = model.Slide4 != null ? model.TitleSlide4 ?? "" : "",
                    DescriptionSlide4 = model.Slide4 != null ? model.DescriptionSlide4 ?? "" : "",

                    TitleSlide5 = model.Slide5 != null ? model.TitleSlide5 ?? "" : "",
                    DescriptionSlide5 = model.Slide5 != null ? model.DescriptionSlide5 ?? "" : "",

                    Slide1 = slide1Path,
                    Slide2 = slide2Path,
                    Slide3 = slide3Path,
                    Slide4 = slide4Path,
                    Slide5 = slide5Path,

                    Image1 = image1Path,
                    Image2 = image2Path,
                    Video = videoPath,

                    Title1 = model.Title1 ?? "",
                    Title2 = model.Title2 ?? "",
                    Description1 = model.Description1 ?? "",
                    Description2 = model.Description2 ?? ""
                };

                var (success, _ignored, message) = await _decorationService.CreateAsync(decorate);

                TempData["CustomToastType"] = success ? "success" : "error";
                TempData["CustomToastMessage"] = success
                    ? "Cập nhật decorate thành công!"
                    : (string.IsNullOrWhiteSpace(message) ? "Tạo decorate thất bại!" : message);

                return Json(new
                {
                    success,
                    redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception tại MVC Decorates/Create:");
                Console.WriteLine(ex);

                TempData["CustomToastType"] = "error";
                TempData["CustomToastMessage"] = "🔥 Server Error: " + ex.Message;

                return Json(new
                {
                    success = false,
                    redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                });
            }
        }


        // Cập nhật decorate 
        [HttpPost]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> Update(int id, [FromForm] DecoratesViewModel model)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("Id");
                if (string.IsNullOrEmpty(userIdStr))
                {
                    TempData["CustomToastType"] = "error";
                    TempData["CustomToastMessage"] = "Bạn chưa đăng nhập!";
                    return Json(new
                    {
                        success = false,
                        redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                    });
                }

                var decorate = id > 0
                    ? await _decorationService.GetDecorateByIdAsync(id)
                    : await _decorationService.GetGlobalDecorateAsync();

                if (decorate == null)
                {
                    TempData["CustomToastType"] = "error";
                    TempData["CustomToastMessage"] = "Decorate không tồn tại!";
                    return Json(new
                    {
                        success = false,
                        redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                    });
                }

                // Chuẩn hóa id
                id = decorate.Id;

                // Upload file nếu có
                if (model.Image1 != null) decorate.Image1 = await SaveFileAsync(model.Image1, "decorates/images");
                if (model.Image2 != null) decorate.Image2 = await SaveFileAsync(model.Image2, "decorates/images");
                if (model.Video != null) decorate.Video = await SaveFileAsync(model.Video, "decorates/videos");

                if (model.Slide1 != null) decorate.Slide1 = await SaveFileAsync(model.Slide1, "decorates/slideshow");
                if (!string.IsNullOrWhiteSpace(model.TitleSlide1)) decorate.TitleSlide1 = model.TitleSlide1;
                if (!string.IsNullOrWhiteSpace(model.DescriptionSlide1)) decorate.DescriptionSlide1 = model.DescriptionSlide1;

                if (model.Slide2 != null) decorate.Slide2 = await SaveFileAsync(model.Slide2, "decorates/slideshow");
                if (!string.IsNullOrWhiteSpace(model.TitleSlide2)) decorate.TitleSlide2 = model.TitleSlide2;
                if (!string.IsNullOrWhiteSpace(model.DescriptionSlide2)) decorate.DescriptionSlide2 = model.DescriptionSlide2;

                if (model.Slide3 != null) decorate.Slide3 = await SaveFileAsync(model.Slide3, "decorates/slideshow");
                if (!string.IsNullOrWhiteSpace(model.TitleSlide3)) decorate.TitleSlide3 = model.TitleSlide3;
                if (!string.IsNullOrWhiteSpace(model.DescriptionSlide3)) decorate.DescriptionSlide3 = model.DescriptionSlide3;

                if (model.Slide4 != null) decorate.Slide4 = await SaveFileAsync(model.Slide4, "decorates/slideshow");
                if (!string.IsNullOrWhiteSpace(model.TitleSlide4)) decorate.TitleSlide4 = model.TitleSlide4;
                if (!string.IsNullOrWhiteSpace(model.DescriptionSlide4)) decorate.DescriptionSlide4 = model.DescriptionSlide4;

                if (model.Slide5 != null) decorate.Slide5 = await SaveFileAsync(model.Slide5, "decorates/slideshow");
                if (!string.IsNullOrWhiteSpace(model.TitleSlide5)) decorate.TitleSlide5 = model.TitleSlide5;
                if (!string.IsNullOrWhiteSpace(model.DescriptionSlide5)) decorate.DescriptionSlide5 = model.DescriptionSlide5;

                // Text phần decorate images
                if (!string.IsNullOrWhiteSpace(model.Title1)) decorate.Title1 = model.Title1;
                if (!string.IsNullOrWhiteSpace(model.Title2)) decorate.Title2 = model.Title2;
                if (!string.IsNullOrWhiteSpace(model.Description1)) decorate.Description1 = model.Description1;
                if (!string.IsNullOrWhiteSpace(model.Description2)) decorate.Description2 = model.Description2;

                var (success, _data, message) = await _decorationService.UpdateAsync(id, decorate);

                TempData["CustomToastType"] = success ? "success" : "error";
                TempData["CustomToastMessage"] = success
                    ? "Cập nhật trang trí thành công!"
                    : (string.IsNullOrWhiteSpace(message) ? "Cập nhật thất bại!" : message);

                return Json(new
                {
                    success,
                    redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception tại MVC Decorates/Update:");
                Console.WriteLine(ex);

                TempData["CustomToastType"] = "error";
                TempData["CustomToastMessage"] = "Server Error: " + ex.Message;

                return Json(new
                {
                    success = false,
                    redirectUrl = Url.Action("Decorates", "Decorates", new { area = "Admin" })
                });
            }
        }



        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            // Sử dụng Google Cloud Storage Service để tải file lên Google Cloud
            var fileUrl = await _googleCloudStorageService.UploadFileAsync(file, folder);
            if (string.IsNullOrEmpty(fileUrl))
            {
                throw new Exception("Không thể tải file lên Google Cloud Storage.");
            }

            return fileUrl;
        }


        // Xóa tất cả 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"ID received: {id}");

            try
            {
                var success = await _decorationService.DeleteDecorateAsync(id);

                if (success)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Xoá trang trí thành công!"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Xoá thất bại. Vui lòng thử lại!"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi DeleteConfirmed: " + ex.Message);
                return Json(new
                {
                    success = false,
                    message = "Server Error: " + ex.Message
                });
            }
        }









        // Đăng xuất
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }


        // Xóa theo từng mục 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllSlides(int id)
        {
            Console.WriteLine($"Xoá slide cho Decorate ID: {id}");

            var result = await _decorationService.DeleteAllSlidesAsync(id);

            return Json(new
            {
                success = result,
                message = result ? "Đã xoá toàn bộ slide!" : "❌ Xoá thất bại!"
            });
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            Console.WriteLine($"Xoá video cho Decorate ID: {id}");

            var result = await _decorationService.DeleteVideoAsync(id);

            return Json(new
            {
                success = result,
                message = result ? "Đã xoá video!" : "❌ Xoá thất bại!"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDecorate1(int id)
        {
            Console.WriteLine($"Xoá decorate 1 cho Decorate ID: {id}");

            var result = await _decorationService.DeleteDecorate1Async(id);

            return Json(new
            {
                success = result,
                message = result ? "Đã xoá ảnh Decorate 1!" : "❌ Xoá thất bại!"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDecorate2(int id)
        {
            Console.WriteLine($"Xoá decorate 2 cho Decorate ID: {id}");

            var result = await _decorationService.DeleteDecorate2Async(id);

            return Json(new
            {
                success = result,
                message = result ? "Đã xoá ảnh Decorate 2!" : "❌ Xoá thất bại!"
            });
        }

    }
}