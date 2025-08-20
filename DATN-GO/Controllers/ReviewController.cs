using DATN_GO.Service;
using DATN_GO.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DATN_GO.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ReviewService _reviewService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(ReviewService reviewService, GoogleCloudStorageService gcsService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService;
            _gcsService = gcsService;
            _logger = logger;
        }

        // Hiển thị tất cả review (admin hoặc debug)
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Bắt đầu lấy tất cả review.");
            var reviews = await _reviewService.GetAllReviewsAsync();
            _logger.LogInformation("Lấy {Count} review thành công.", reviews?.Count ?? 0);
            return View(reviews);
        }

        // Hiển thị review chi tiết
        public async Task<IActionResult> Detail(int id)
        {
            _logger.LogInformation("Lấy chi tiết review với Id: {ReviewId}", id);
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
            {
                _logger.LogWarning("Không tìm thấy review với Id: {ReviewId}", id);
                TempData["ToastMessage"] = "Không tìm thấy đánh giá.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index");
            }
            _logger.LogInformation("Lấy chi tiết review thành công với Id: {ReviewId}", id);
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewCreateRequest model, List<IFormFile>? mediaFiles)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                _logger.LogWarning("Người dùng chưa đăng nhập khi tạo review cho ProductId={ProductId}", model.ProductId);
                TempData["ToastMessage"] = "Bạn cần đăng nhập để đánh giá sản phẩm.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            model.UserId = userId;
            _logger.LogInformation("Người dùng đang tạo review. UserId={UserId}, ProductId={ProductId}", userId, model.ProductId);
            _logger.LogInformation("Số file media nhận được: {Count}", mediaFiles?.Count ?? 0);

            var uploadedMedias = new List<string>();

            // Upload ảnh review
            if (mediaFiles != null && mediaFiles.Count > 0)
            {
                foreach (var file in mediaFiles)
                {
                    if (file != null && file.Length > 0)
                    {
                        try
                        {
                            var url = await _gcsService.UploadFileAsync(file, "reviews/");
                            if (!string.IsNullOrEmpty(url))
                            {
                                uploadedMedias.Add(url);
                                _logger.LogInformation("Uploaded review media URL: {Url}", url);
                            }
                            else
                            {
                                _logger.LogError("Upload ảnh review thất bại: {FileName}", file.FileName);
                                TempData["ToastMessage"] = $"Tải ảnh {file.FileName} thất bại. Vui lòng thử lại.";
                                TempData["ToastType"] = "danger";
                                return RedirectToAction("DetailProducts", "Products", new { id = model.ProductId });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi upload ảnh review {FileName}", file.FileName);
                            TempData["ToastMessage"] = $"Lỗi tải ảnh {file.FileName}.";
                            TempData["ToastType"] = "danger";
                            return RedirectToAction("DetailProducts", "Products", new { id = model.ProductId });
                        }
                    }
                }
            }

            model.MediaList = uploadedMedias;
            _logger.LogInformation("Payload review trước khi tạo: {@ReviewRequest}", model);

            try
            {
                var review = await _reviewService.CreateReviewAsync(model);
                if (review != null)
                {
                    _logger.LogInformation("Tạo review thành công. ReviewId={ReviewId}", review.Id);
                    TempData["ToastMessage"] = "Đánh giá sản phẩm thành công!";
                    TempData["ToastType"] = "success";
                }
                else
                {
                    _logger.LogWarning("Tạo review thất bại. Payload={@ReviewRequest}", model);
                    TempData["ToastMessage"] = "Gửi đánh giá thất bại.";
                    TempData["ToastType"] = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo review. Payload: {@ReviewRequest}", model);
                TempData["ToastMessage"] = "Gửi đánh giá thất bại. Xảy ra lỗi server.";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("DetailProducts", "Products", new { id = model.ProductId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Bắt đầu xóa review Id: {ReviewId}", id);

            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(Encoding.UTF8.GetString(idBytes), out int userId))
            {
                _logger.LogWarning("Người dùng chưa đăng nhập, không thể xóa review.");
                TempData["ToastMessage"] = "Bạn cần đăng nhập để thao tác.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            var success = await _reviewService.DeleteReviewAsync(id);
            if (success)
            {
                _logger.LogInformation("Xóa review Id: {ReviewId} thành công.", id);
                TempData["ToastMessage"] = "Xóa đánh giá thành công.";
                TempData["ToastType"] = "success";
            }
            else
            {
                _logger.LogWarning("Xóa review Id: {ReviewId} thất bại.", id);
                TempData["ToastMessage"] = "Xóa đánh giá thất bại.";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("Index");
        }
    }
}