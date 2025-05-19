using DATN_GO.Models;
using DATN_GO.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using DATN_GO.ViewModels;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace DATN_GO.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class PostArticleController : Controller
    {
        private readonly PostService _postService;
        private readonly GoogleCloudStorageService _gcsService;
        private readonly UserService _userService;
        private readonly ILogger<PostArticleController> _logger;

        public PostArticleController(PostService postService, UserService userService, GoogleCloudStorageService gcsService, ILogger<PostArticleController> logger)
        {
            _postService = postService;
            _gcsService = gcsService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<IActionResult> PostArticle(string? search, string? sortOrder)
        {
            try
            {
                if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
                {
                    _logger.LogWarning("User ID not found in session or invalid. Redirecting to Login.");
                    TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("PostArticle");
                }

                var user = await _userService.GetUserByIdAsync(userId);
                var posts = await _postService.GetPostsByUserIdAsync(userId);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    posts = posts.Where(p => p.Content != null && p.Content.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Sắp xếp theo thời gian
                if (sortOrder == "oldest")
                {
                    posts = posts.OrderBy(p => p.CreateAt).ToList();
                }
                else
                {
                    posts = posts.OrderByDescending(p => p.CreateAt).ToList();
                }
                foreach (var post in posts)
                {
                    post.User = user;
                }

                var viewModel = new PostArticleViewModel
                {
                    CurrentUser = user,
                    Post = new Posts(),
                    PostList = posts.OrderByDescending(p => p.CreateAt).ToList()
                };

                ViewBag.CurrentUser = user;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải bài đăng.");
                TempData["ToastMessage"] = "Có lỗi xảy ra khi tải bài đăng.";
                TempData["ToastType"] = "danger";
                return View(new PostArticleViewModel());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(PostArticleViewModel model, IFormFile imageFile)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) || !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("PostArticle");
            }

            if (string.IsNullOrWhiteSpace(model.Post.Content))
            {
                TempData["ToastMessage"] = "Nội dung bài viết không được để trống.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("PostArticle");
            }

            var post = new Posts
            {
                Content = model.Post.Content.Trim(),
                CreateAt = DateTime.Now.AddHours(7),
                UserId = userId
            };

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    post.Image = await _gcsService.UploadFileAsync(imageFile, "posts/");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tải ảnh lên GCS.");
                    TempData["ToastMessage"] = "Tải ảnh không thành công.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("PostArticle");
                }
            }

            await _postService.CreatePostAsync(post);

            TempData["ToastMessage"] = "Đăng bài viết thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("PostArticle");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _postService.GetPostByIdAsync(id);
            if (post == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy bài viết.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("PostArticle");
            }

            return View(post);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int Id, string Content, IFormFile imageFile)
        {
            var post = await _postService.GetPostByIdAsync(Id);
            if (post == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy bài viết cần chỉnh sửa.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("PostArticle");
            }

            if (string.IsNullOrWhiteSpace(Content))
            {
                TempData["ToastMessage"] = "Nội dung không được để trống.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("PostArticle");
            }

            post.Content = Content.Trim();

            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    post.Image = await _gcsService.UploadFileAsync(imageFile, "posts/");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể tải ảnh mới.");
                    TempData["ToastMessage"] = "Không thể cập nhật ảnh mới.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("PostArticle");
                }
            }

            await _postService.UpdatePostAsync(post.Id, post);
            TempData["ToastMessage"] = "Cập nhật bài viết thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("PostArticle");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _postService.DeletePostAsync(id);
            if (success)
            {
                TempData["ToastMessage"] = "Xoá bài viết thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Xoá bài viết thất bại.";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("PostArticle");
        }
    }
}
