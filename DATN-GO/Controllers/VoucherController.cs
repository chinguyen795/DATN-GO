using DATN_GO.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DATN_GO.Controllers
{
    public class VoucherController : Controller
    {
        private readonly HttpClient _httpClient;

        public VoucherController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7096/api/vouchers/"); // Đổi nếu API khác port
        }

        // View chính
        [HttpGet]
        public async Task<IActionResult> Voucher()
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                TempData["ToastMessage"] = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Login", "UserAuthentication");
            }

            var user = await GetUserById(userId); // gọi hàm riêng lấy thông tin user
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy thông tin người dùng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            return View(user); // truyền user vào View
        }

        private async Task<Users?> GetUserById(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:7096/api/users/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Users>(jsonString);
                }
            }
            catch (Exception ex)
            {
                // Log nếu cần
            }
            return null;
        }

        // API để lấy voucher đã lưu của user
        [HttpGet]
        [ActionName("AllApi")]
        public async Task<IActionResult> AllApi()
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                return Json(new List<object>());
            }

            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:7096/api/UserVouchers/user/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new List<object>());
                }

                var json = await response.Content.ReadAsStringAsync();

                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        // API để lưu voucher
        [HttpPost]
        [ActionName("SaveVoucher")]
        public async Task<IActionResult> SaveVoucher([FromBody] SaveVoucherRequest request)
        {
            if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập" });
            }

            request.UserId = userId;

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://localhost:7096/api/UserVouchers/save", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Lưu voucher thành công" });
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseJson);
                return Json(new { success = false, message = errorResponse?.message?.ToString() ?? "Có lỗi xảy ra" });
            }
        }

        [HttpGet]
        [ActionName("CheckSaved")]
        public async Task<IActionResult> CheckSaved(int voucherId)
        {
            try
            {
                if (!HttpContext.Session.TryGetValue("Id", out byte[] idBytes) ||
                    !int.TryParse(System.Text.Encoding.UTF8.GetString(idBytes), out int userId))
                {
                    return Json(new { isSaved = false, message = "User not logged in" });
                }

                var response = await _httpClient.GetAsync($"https://localhost:7096/api/UserVouchers/check/{userId}/{voucherId}");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { isSaved = false, message = $"API call failed: {response.StatusCode}" });
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);

                // Đảm bảo trả về đúng format
                bool isSaved = result?.isSaved == true;
                return Json(new { isSaved = isSaved });
            }
            catch (Exception ex)
            {
                return Json(new { isSaved = false, message = $"Error: {ex.Message}" });
            }
        }

        // Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            TempData["ToastMessage"] = "Bạn đã đăng xuất thành công!";
            TempData["ToastType"] = "success";

            return RedirectToAction("Index", "Home", new { area = "" });
        }


    }

    public class SaveVoucherRequest
    {
        public int UserId { get; set; }
        public int VoucherId { get; set; }
    }
}