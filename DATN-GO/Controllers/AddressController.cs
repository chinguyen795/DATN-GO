using DATN_GO.Models;
using DATN_GO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

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
            _httpClient = factory.CreateClient("api"); // đã cấu hình base address trong Program.cs
        }
        public async Task<IActionResult> Address()
        {
            var addresses = await _service.GetAddressesAsync();
            var users = await GetUsersAsync();

            // Ghép user info vào address
            foreach (var address in addresses)
            {
                var user = users.FirstOrDefault(u => u.Id == address.UserId);
                if (user != null)
                {
                    // Gán tạm User (dù có JsonIgnore cũng không ảnh hưởng vì bạn render ở View)
                    address.User = user;
                }
            }

            // Lọc bỏ địa chỉ hệ thống nếu cần
            var userAddresses = addresses
                .Where(a => a.Discription == null || !a.Discription.StartsWith("Tự động tạo cho"))
                .ToList();

            return View(userAddresses);
        }



        public async Task<IActionResult> Create()
        {
            var users = await GetUsersAsync();
            ViewBag.Users = new SelectList(users, "Id", "FullName");
            return View(new Addresses());
        }
        [HttpPost]
        public async Task<IActionResult> Create(Addresses model)
        {
            if (model.UserId == null || model.UserId <= 0)
                ModelState.AddModelError("UserId", "Vui lòng chọn người dùng hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.Discription))
                ModelState.AddModelError("Discription", "Vui lòng chọn địa chỉ trên bản đồ.");
            if (model.Latitude == 0 || model.Longitude == 0)
                ModelState.AddModelError("Latitude", "Vui lòng chọn vị trí trên bản đồ.");

            var users = await GetUsersAsync();
            ViewBag.Users = new SelectList(users, "Id", "FullName", model.UserId);

            if (!ModelState.IsValid)
                return View(model);

            // Nếu là mặc định, kiểm tra xem đã có chưa
            if (model.Status == "Mặc định")
            {
                var allAddresses = await _service.GetAddressesAsync();
                var existingDefault = allAddresses
                    .FirstOrDefault(a => a.UserId == model.UserId && a.Status == "Mặc định");

                if (existingDefault != null)
                {
                    // gửi thông tin về cho view để bật modal
                    ViewBag.ShowDefaultModal = true;
                    ViewBag.ExistingDefault = existingDefault.Discription;
                    TempData["PendingAddress"] = JsonConvert.SerializeObject(model);
                    return View(model);
                }
            }

            var result = await _service.AddAddressAsync(model);
            if (result)
            {
                TempData["Success"] = "Thêm địa chỉ thành công!";
                return RedirectToAction("Address");
            }

            ModelState.AddModelError("", "Đã có lỗi khi lưu địa chỉ.");
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
            var success = await _service.DeleteAddressAsync(id);
            if (success)
                TempData["Success"] = "Xoá địa chỉ thành công!";
            else
                TempData["Error"] = "Xoá địa chỉ thất bại!";

            return RedirectToAction("Address"); // hoặc "Index" nếu bạn dùng action đó
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

            int? cityId = null;
            int? districtId = null;
            int? wardId = null;

            // Tạm suy luận từ Discription nếu không có WardId trong model
            wardId = await TryGetWardIdFromDescriptionAsync(address.Discription);

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

            ViewBag.Users = new SelectList(await GetUsersAsync(), "Id", "FullName", address.UserId);
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
                TempData["Error"] = "Không tìm thấy địa chỉ để đặt mặc định.";
                return RedirectToAction("Address");
            }

            var userId = addressToSet.UserId;

            // Cập nhật tất cả các địa chỉ của user thành "Không mặc định"
            foreach (var addr in allAddresses.Where(a => a.UserId == userId))
            {
                if (addr.Status == "Mặc định")
                {
                    addr.Status = "Không mặc định";
                    await _service.UpdateAddressAsync(addr);
                }
            }

            // Đặt địa chỉ mới là mặc định
            addressToSet.Status = "Mặc định";
            await _service.UpdateAddressAsync(addressToSet);

            TempData["Success"] = "Đã cập nhật địa chỉ mặc định.";
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
                : "Đã thêm địa chỉ mới không phải mặc định.";

            return RedirectToAction("Address");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Addresses model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = new SelectList(await GetUsersAsync(), "Id", "FullName", model.UserId);
                return View(model);
            }

            var success = await _service.UpdateAddressAsync(model);
            if (success)
            {
                TempData["Success"] = "Cập nhật địa chỉ thành công!";
                return RedirectToAction("Address");
            }

            TempData["Error"] = "Cập nhật địa chỉ thất bại!";
            ViewBag.Users = new SelectList(await GetUsersAsync(), "Id", "FullName", model.UserId);
            return View(model);
        }


    }
}
