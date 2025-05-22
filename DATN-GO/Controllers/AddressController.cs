using DATN_GO.Models;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace DATN_GO.Controllers
{
    public class AddressController : Controller
    {
        private readonly AddressService _service;
        private readonly HttpClient _httpClient;

        public class WardDto
        {
            public int Id { get; set; }
            public string WardName { get; set; }
            public int DistrictId { get; set; }
        }

        public class DistrictDto
        {
            public int Id { get; set; }
            public string DistrictName { get; set; }
            public int CityId { get; set; }
        }

        public AddressController(AddressService service, IHttpClientFactory factory)
        {
            _service = service;
            _httpClient = factory.CreateClient("api");
        }
        public async Task<IActionResult> Address()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Id")))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                TempData["ToastType"] = "warning";
                TempData["TriggerLoginModal"] = true;
                return RedirectToAction("Index", "Home");
            }

            int.TryParse(HttpContext.Session.GetString("Id"), out var currentUserId);
            var addresses = await _service.GetAddressesAsync();

            // ✅ Lọc địa chỉ của user hiện tại
            var userAddresses = addresses
                .Where(a => a.UserId == currentUserId &&
                       (a.Discription == null || !a.Discription.StartsWith("Tự động tạo cho")))
                .ToList();

            // Gắn info user
            var users = await GetUsersAsync();
            foreach (var address in userAddresses)
            {
                address.User = users.FirstOrDefault(u => u.Id == address.UserId);
            }

            return View(userAddresses);
        }





        // GET: Create
        public async Task<IActionResult> Create()
        {
            // ✅ Kiểm tra đăng nhập
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Id")))
            {
                TempData["TriggerLoginModal"] = true;
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", "Home");

            }

            var users = await GetUsersAsync();

            int.TryParse(HttpContext.Session.GetString("Id"), out var currentUserId);
            var fullName = HttpContext.Session.GetString("FullName");

            ViewBag.CurrentUserName = fullName;

            return View(new Addresses { UserId = currentUserId });
        }


        // POST: Create
        [HttpPost]
        public async Task<IActionResult> Create(Addresses model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Id")))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                TempData["ToastType"] = "warning";
                TempData["TriggerLoginModal"] = true;
                return RedirectToAction("Index", "Home");
            }

            var displayName = Request.Form["DisplayName"].ToString();
            var phoneNumber = Request.Form["PhoneNumber"].ToString();

            // 🔎 Kiểm tra rỗng
            if (model.UserId == 0)
            {
                TempData["ToastMessage"] = "Thiếu thông tin người dùng";
                TempData["ToastType"] = "danger";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                TempData["ToastMessage"] = "Tên người nhận không được để trống";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            // 🔒 Không ký tự đặc biệt, không số (cho phép chữ có dấu và khoảng trắng)
            if (!System.Text.RegularExpressions.Regex.IsMatch(displayName, @"^[\p{L}\p{M}\s]+$"))
            {
                TempData["ToastMessage"] = "Tên người nhận không được chứa số hoặc ký tự đặc biệt.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["ToastMessage"] = "Số điện thoại không được để trống.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            if (!phoneNumber.All(char.IsDigit) || phoneNumber.Length != 10)
            {
                TempData["ToastMessage"] = "Số điện thoại phải đúng 10 chữ số.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Discription))
            {
                TempData["ToastMessage"] = "Vui lòng chọn địa chỉ trên bản đồ.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            if (model.Latitude == 0 || model.Longitude == 0)
            {
                TempData["ToastMessage"] = "Vui lòng chọn vị trí trên bản đồ.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            // 🔄 Cập nhật người dùng nếu cần
            var users = await GetUsersAsync();
            var user = users.FirstOrDefault(u => u.Id == model.UserId);
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy người dùng.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Address");
            }

            var allAddresses = await _service.GetAddressesAsync();
            var userAddresses = allAddresses.Where(a => a.UserId == model.UserId).ToList();

            if (userAddresses.Count >= 5)
            {
                TempData["ToastMessage"] = "Bạn chỉ có thể thêm tối đa 5 địa chỉ.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Create");
            }

            bool shouldUpdate = false;

            if (displayName != user.FullName)
            {
                user.FullName = displayName;
                HttpContext.Session.SetString("FullName", displayName);
                shouldUpdate = true;
            }

            if (phoneNumber != user.PhoneNumber)
            {
                user.PhoneNumber = phoneNumber;
                HttpContext.Session.SetString("PhoneNumber", phoneNumber);
                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                var updateRes = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user);
                if (!updateRes.IsSuccessStatusCode)
                {
                    TempData["ToastMessage"] = "Cập nhật thông tin người dùng thất bại.";
                    TempData["ToastType"] = "danger";
                    return View(model);
                }
            }

            if (model.Status == "Mặc định")
            {
                var existingDefault = userAddresses.FirstOrDefault(a => a.Status == "Mặc định");
                if (existingDefault != null)
                {
                    ViewBag.ShowDefaultModal = true;
                    ViewBag.ExistingDefault = existingDefault.Discription;
                    TempData["PendingAddress"] = JsonConvert.SerializeObject(model);
                    return View(model);
                }
            }

            var result = await _service.AddAddressAsync(model);
            if (result)
            {
                TempData["ToastMessage"] = "Thêm địa chỉ thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Address");
            }

            TempData["ToastMessage"] = "Đã có lỗi khi lưu địa chỉ.";
            TempData["ToastType"] = "danger";
            return View(model);
        }







        private async Task<List<Users>> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync("api/users");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Users>>(json) ?? new();
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var address = await _service.GetAddressByIdAsync(id);
            if (address == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy địa chỉ";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Address");
            }

            var userId = address.UserId;
            var allAddresses = (await _service.GetAddressesAsync())
                .Where(a => a.UserId == userId)
                .ToList();

            // ❌ Nếu chỉ có 1 địa chỉ duy nhất
            if (allAddresses.Count == 1)
            {
                TempData["ToastMessage"] = "Bạn phải có ít nhất 1 địa chỉ";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Address");
            }

            // ❌ Nếu là địa chỉ mặc định, không cho xoá
            if (address.Status == "Mặc định")
            {
                TempData["ToastMessage"] = "Không thể xoá địa chỉ mặc định";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Address");
            }

            // ✅ Cho xoá nếu không phải mặc định
            var success = await _service.DeleteAddressAsync(id);
            if (success)
            {
                TempData["ToastMessage"] = "Xoá địa chỉ thành công!";
                TempData["ToastType"] = "success";
            }
            else
            {
                TempData["ToastMessage"] = "Xoá địa chỉ thất bại!";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("Address");
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var address = await _service.GetAddressByIdAsync(id);
            if (address == null)
            {
                TempData["Error"] = "Không tìm thấy địa chỉ!";
                return RedirectToAction("Address");
            }

            // 🔥 Lấy tên và SĐT từ session giống Create
            var fullName = HttpContext.Session.GetString("FullName");
            var phone = HttpContext.Session.GetString("PhoneNumber");

            ViewBag.CurrentUserName = fullName;
            ViewBag.CurrentUserPhone = phone;

            // Gắn user cho Address nếu cần
            var users = await GetUsersAsync();
            address.User = users.FirstOrDefault(u => u.Id == address.UserId);

            // Optional: Lấy thông tin City/District/Ward nếu dùng
            int? cityId = null, districtId = null, wardId = await TryGetWardIdFromDescriptionAsync(address.Discription);
            if (wardId.HasValue)
            {
                var wardRes = await _httpClient.GetAsync($"api/wards/{wardId}");
                if (wardRes.IsSuccessStatusCode)
                {
                    var wardJson = await wardRes.Content.ReadAsStringAsync();
                    var ward = JsonConvert.DeserializeObject<WardDto>(wardJson);
                    districtId = ward?.DistrictId;

                    if (districtId.HasValue)
                    {
                        var distRes = await _httpClient.GetAsync($"api/districts/{districtId}");
                        if (distRes.IsSuccessStatusCode)
                        {
                            var distJson = await distRes.Content.ReadAsStringAsync();
                            var district = JsonConvert.DeserializeObject<DistrictDto>(distJson);
                            cityId = district?.CityId;
                        }
                    }
                }
            }

            ViewBag.CityId = cityId;
            ViewBag.DistrictId = districtId;
            ViewBag.WardId = wardId;

            return View(address);
        }






        // Tìm wardId từ địa chỉ text (Discription)
        private async Task<int?> TryGetWardIdFromDescriptionAsync(string discription)
        {
            if (string.IsNullOrWhiteSpace(discription)) return null;

            var response = await _httpClient.GetAsync("api/wards");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var allWards = JsonConvert.DeserializeObject<List<WardDto>>(json);
            if (allWards == null) return null;

            foreach (var ward in allWards)
            {
                if (discription.Contains(ward.WardName, StringComparison.OrdinalIgnoreCase))
                    return ward.Id;
            }

            return null;
        }


        // Đặt địa chỉ làm mặc định
        [HttpPost]
        public async Task<IActionResult> SetDefault(int id)
        {
            var allAddresses = await _service.GetAddressesAsync();
            var addressToSet = allAddresses.FirstOrDefault(a => a.Id == id);

            if (addressToSet == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy địa chỉ để đặt mặc định.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Address");
            }

            var userId = addressToSet.UserId;

            // Cập nhật các địa chỉ khác thành "Không mặc định"
            foreach (var addr in allAddresses.Where(a => a.UserId == userId))
            {
                if (addr.Status == "Mặc định")
                {
                    addr.Status = "Không mặc định";
                    await _service.UpdateAddressAsync(addr);
                }
            }

            // Cập nhật địa chỉ được chọn thành "Mặc định"
            addressToSet.Status = "Mặc định";
            await _service.UpdateAddressAsync(addressToSet);

            TempData["ToastMessage"] = "Đã cập nhật địa chỉ mặc định!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Address");
        }

        // Xác nhận mặc định

        [HttpPost]
        public async Task<IActionResult> ConfirmReplaceDefault(bool confirm)
        {
            if (TempData["PendingAddress"] == null)
            {
                TempData["Error"] = "Không tìm thấy địa chỉ cần xác nhận.";
                return RedirectToAction("Address");
            }

            var pending = JsonConvert.DeserializeObject<Addresses>(TempData["PendingAddress"]!.ToString());
            if (pending == null)
            {
                TempData["Error"] = "Dữ liệu địa chỉ không hợp lệ.";
                return RedirectToAction("Address");
            }

            var allAddresses = await _service.GetAddressesAsync();

            if (confirm)
            {
                // ✅ Nếu chọn CÓ: reset mặc định cũ
                foreach (var addr in allAddresses.Where(a => a.UserId == pending.UserId && a.Status == "Mặc định"))
                {
                    addr.Status = "Không mặc định";
                    await _service.UpdateAddressAsync(addr);
                }
            }
            else
            {
                // ❌ Nếu chọn KHÔNG: ép status về "Không mặc định"
                pending.Status = "Không mặc định";
            }

            var result = await _service.AddAddressAsync(pending);
            TempData["Success"] = confirm
                ? "Đã thay thế địa chỉ mặc định."
                : "Đã thêm địa chỉ mới không phải mặc định";

            return RedirectToAction("Address");
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Addresses model)
        {
            var displayName = Request.Form["DisplayName"].ToString();
            var phoneNumber = Request.Form["PhoneNumber"].ToString();
            ViewBag.CurrentUserName = displayName;
            ViewBag.CurrentUserPhone = phoneNumber;

            // 👉 Bắt lỗi tên trống
            if (string.IsNullOrWhiteSpace(displayName))
            {
                TempData["ToastMessage"] = "Tên người nhận không được để trống.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            // 👉 Bắt lỗi tên chứa ký tự đặc biệt hoặc số
            if (!System.Text.RegularExpressions.Regex.IsMatch(displayName, @"^[\p{L}\p{M}\s]+$"))
            {
                TempData["ToastMessage"] = "Tên người nhận không được chứa số hoặc ký tự đặc biệt.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            // 👉 Bắt lỗi số điện thoại trống
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["ToastMessage"] = "Số điện thoại không được để trống.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            // 👉 Bắt lỗi số điện thoại không đúng định dạng 10 số
            if (!phoneNumber.All(char.IsDigit) || phoneNumber.Length != 10)
            {
                TempData["ToastMessage"] = "Số điện thoại phải gồm đúng 10 chữ số.";
                TempData["ToastType"] = "warning";
                return View(model);
            }

            var users = await GetUsersAsync();
            var user = users.FirstOrDefault(u => u.Id == model.UserId);
            if (user != null)
            {
                bool needUpdate = false;

                if (!string.IsNullOrWhiteSpace(displayName) && displayName != user.FullName)
                {
                    user.FullName = displayName;
                    HttpContext.Session.SetString("FullName", displayName);
                    needUpdate = true;
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber != user.PhoneNumber)
                {
                    user.PhoneNumber = phoneNumber;
                    HttpContext.Session.SetString("PhoneNumber", phoneNumber);
                    needUpdate = true;
                }

                if (needUpdate)
                {
                    var updateRes = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user);
                    if (!updateRes.IsSuccessStatusCode)
                    {
                        TempData["ToastMessage"] = "Cập nhật thông tin người dùng thất bại.";
                        TempData["ToastType"] = "danger";
                        return View(model);
                    }
                }
            }

            var allAddresses = await _service.GetAddressesAsync();

            if (model.Status == "Không mặc định")
            {
                var hasDefault = allAddresses.Any(a => a.UserId == model.UserId && a.Id != model.Id && a.Status == "Mặc định");
                if (!hasDefault)
                {
                    TempData["ToastMessage"] = "Phải có ít nhất 1 địa chỉ mặc định.";
                    TempData["ToastType"] = "warning";
                    return View(model);
                }
            }

            if (model.Status == "Mặc định")
            {
                var existingDefault = allAddresses.FirstOrDefault(a => a.UserId == model.UserId && a.Status == "Mặc định" && a.Id != model.Id);
                if (existingDefault != null)
                {
                    ViewBag.ShowDefaultModal = true;
                    ViewBag.ExistingDefault = existingDefault.Discription;
                    TempData["PendingEditAddress"] = JsonConvert.SerializeObject(model);
                    return View(model);
                }
            }

            var success = await _service.UpdateAddressAsync(model);
            if (success)
            {
                TempData["ToastMessage"] = "Cập nhật địa chỉ thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Address");
            }

            TempData["ToastMessage"] = "Cập nhật địa chỉ thất bại!";
            TempData["ToastType"] = "danger";
            return View(model);
        }







        [HttpPost]
        public async Task<IActionResult> ConfirmReplaceDefaultEdit(bool confirm)
        {
            if (TempData["PendingEditAddress"] == null)
            {
                TempData["Error"] = "Không tìm thấy địa chỉ để xác nhận cập nhật.";
                return RedirectToAction("Address");
            }

            var pending = JsonConvert.DeserializeObject<Addresses>(TempData["PendingEditAddress"].ToString());
            if (pending == null)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Address");
            }

            var allAddresses = await _service.GetAddressesAsync();

            if (confirm)
            {
                // Reset tất cả địa chỉ mặc định cũ
                foreach (var addr in allAddresses.Where(a => a.UserId == pending.UserId && a.Status == "Mặc định" && a.Id != pending.Id))
                {
                    addr.Status = "Không mặc định";
                    await _service.UpdateAddressAsync(addr);
                }
            }
            else
            {
                pending.Status = "Không mặc định";
            }

            var success = await _service.UpdateAddressAsync(pending);
            TempData["Success"] = confirm
                ? "Đã thay thế địa chỉ mặc định thành công."
                : "Đã cập nhật địa chỉ ở chế độ không mặc định.";

            return RedirectToAction("Address");
        }



    }
}
