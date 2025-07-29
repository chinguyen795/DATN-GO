using DATN_GO.Models;
using DATN_GO.Services;
using DATN_GO.ViewModels.Address;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DATN_GO.Controllers
{
    public class AddressController : Controller
    {
        private readonly AddressService _service;
        private readonly HttpClient _httpClient;

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

            var userAddresses = addresses
                .Where(a => a.UserId == currentUserId &&
                            (a.Description == null || !a.Description.StartsWith("Tự động tạo cho")))
                .ToList();

            // Gọi 3 API location
            var wardRes = await _httpClient.GetAsync("https://localhost:7096/api/wards");
            var districtRes = await _httpClient.GetAsync("https://localhost:7096/api/districts");
            var cityRes = await _httpClient.GetAsync("https://localhost:7096/api/cities");

            if (!wardRes.IsSuccessStatusCode || !districtRes.IsSuccessStatusCode || !cityRes.IsSuccessStatusCode)
            {
                return View(new List<AddressViewModel>());
            }

            var wards = JsonConvert.DeserializeObject<List<WardViewModel>>(await wardRes.Content.ReadAsStringAsync());
            var districts = JsonConvert.DeserializeObject<List<DistrictViewModel>>(await districtRes.Content.ReadAsStringAsync());
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(await cityRes.Content.ReadAsStringAsync());

            // Mapping từ entity → viewmodel
            var addressViewModels = new List<AddressViewModel>();

            foreach (var addr in userAddresses)
            {
                var city = cities.FirstOrDefault(c => c.Id == addr.Id); // Tạm map theo addr.Id = city.Id
                var district = districts.FirstOrDefault(d => d.CityId == city?.Id);
                var ward = wards.FirstOrDefault(w => w.DistrictId == district?.Id);

                addressViewModels.Add(new AddressViewModel
                {
                    Id = addr.Id,
                    UserId = addr.UserId,
                    Name = addr.Name ?? string.Empty,
                    Phone = addr.Phone ?? string.Empty,
                    Latitude = addr.Latitude,
                    Longitude = addr.Longitude,
                    Description = addr.Description,
                    Status = addr.Status,
                    CityName = city?.CityName,
                    DistrictName = district?.DistrictName,
                    WardName = ward?.WardName
                });
            }

            return View(addressViewModels);
        }


        // GET: Create
        [HttpGet]
        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Id")))
            {
                TempData["TriggerLoginModal"] = true;
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            int.TryParse(HttpContext.Session.GetString("Id"), out var currentUserId);
            var fullName = HttpContext.Session.GetString("FullName") ?? string.Empty;
            var phone = HttpContext.Session.GetString("PhoneNumber") ?? string.Empty;

            var vm = new AddressCreateViewModel
            {
                UserId = currentUserId,
                Name = fullName,
                Phone = phone,
                Latitude = 0,
                Longitude = 0,
                Cities = new(), // để trống, không dùng
                Districts = new(),
                Wards = new()
            };

            return View(vm);
        }





        // POST: Create
        [HttpPost]
        public async Task<IActionResult> Create(AddressCreateViewModel model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Id")))
            {
                TempData["TriggerLoginModal"] = true;
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            model.Name = model.Name?.Trim();
            model.Phone = model.Phone?.Trim();
            model.Description = model.Description?.Trim();

            if (model.UserId == 0 || string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ToastMessage"] = "Thông tin người dùng không hợp lệ";
                TempData["ToastType"] = "danger";
                return await ReloadViewAsync(model);
            }

            var allAddresses = await _service.GetAddressesAsync();
            var userAddresses = allAddresses.Where(a => a.UserId == model.UserId).ToList();

            if (userAddresses.Count >= 5)
            {
                TempData["ToastMessage"] = "Bạn chỉ có thể thêm tối đa 5 địa chỉ";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Create");
            }

            // ✅ Nếu thêm mặc định, thì gỡ mặc định cũ (nếu có)
            if (model.Status == AddressStatus.Default)
            {
                var existingDefault = userAddresses.FirstOrDefault(a => a.Status == AddressStatus.Default);
                if (existingDefault != null)
                {
                    existingDefault.Status = AddressStatus.NotDefault;
                    await _service.UpdateAddressAsync(existingDefault);
                }
            }

            var address = new Addresses
            {
                UserId = model.UserId,
                Name = model.Name,
                Phone = model.Phone,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Description = model.Description,
                Status = model.Status,
                CreateAt = DateTime.Now
            };

            var (success, errorMessage, newAddressId) = await _service.AddAddressAndReturnIdAsync(address);

            if (!success)
            {
                TempData["ToastMessage"] = $"Lỗi khi lưu địa chỉ: {errorMessage}";
                TempData["ToastType"] = "danger";
                return await ReloadViewAsync(model);
            }

            // ✅ Save city → district → ward
            try
            {
                var cityModel = new CityViewModel
                {
                    Id = newAddressId,
                    CityName = model.CityName?.Trim() ?? string.Empty
                };
                await SaveCityAsync(cityModel);

                var districtModel = new DistrictViewModel
                {
                    CityId = cityModel.Id,
                    DistrictName = model.DistrictName?.Trim() ?? string.Empty
                };
                var createdDistrict = await SaveDistrictAsync(districtModel);

                if (createdDistrict.Id == 0)
                    throw new Exception("DistrictId is 0! Không thể tạo Ward nếu không có DistrictId hợp lệ!");

                var wardModel = new WardViewModel
                {
                    WardName = model.WardName?.Trim() ?? string.Empty,
                    DistrictId = createdDistrict.Id
                };
                await SaveWardAsync(wardModel);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Lỗi khi lưu khu vực: {ex.Message}";
                TempData["ToastType"] = "danger";
                return RedirectToAction("Address");
            }

            TempData["ToastMessage"] = "Thêm địa chỉ thành công!";
            TempData["ToastType"] = "success";
            return RedirectToAction("Address");
        }






        private async Task SaveCityAsync(CityViewModel city)
        {
            var check = await _httpClient.GetAsync($"https://localhost:7096/api/cities/{city.Id}");

            if (check.IsSuccessStatusCode)
            {
                // ✅ Update
                var response = await _httpClient.PutAsJsonAsync($"https://localhost:7096/api/cities/{city.Id}", city);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new Exception($"❌ City Update Error: {content}");
                }
            }
            else
            {
                // ✅ Create — PHẢI truyền Id vì dùng shared PK
                var payload = new
                {
                    Id = city.Id, // Address.Id bắt buộc
                    city.CityName
                };

                var response = await _httpClient.PostAsJsonAsync("https://localhost:7096/api/cities", payload);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"❌ City Create Error: {content}");
            }
        }


        private async Task<DistrictViewModel> SaveDistrictAsync(DistrictViewModel district)
        {
            var checkUrl = $"https://localhost:7096/api/districts/by-name?name={Uri.EscapeDataString(district.DistrictName)}&cityId={district.CityId}";
            var check = await _httpClient.GetAsync(checkUrl);

            if (check.IsSuccessStatusCode)
            {
                var json = await check.Content.ReadAsStringAsync();
                var existing = JsonConvert.DeserializeObject<DistrictViewModel>(json)!;
                district.Id = existing.Id; // ✅ gán lại Id
                return existing;
            }

            var payload = new
            {
                district.DistrictName,
                district.CityId
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:7096/api/districts", payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"❌ District Create Error: {content}");

            var created = JsonConvert.DeserializeObject<DistrictViewModel>(content)!;
            district.Id = created.Id; // ✅ gán lại Id
            return created;
        }








        private async Task<WardViewModel> SaveWardAsync(WardViewModel ward)
        {
            var checkUrl = $"https://localhost:7096/api/wards/by-name?name={Uri.EscapeDataString(ward.WardName)}&districtId={ward.DistrictId}";
            var check = await _httpClient.GetAsync(checkUrl);

            if (check.IsSuccessStatusCode)
            {
                var json = await check.Content.ReadAsStringAsync();
                var existing = JsonConvert.DeserializeObject<WardViewModel>(json)!;
                ward.Id = existing.Id; // ✅ gán lại Id
                return existing;
            }

            var payload = new
            {
                ward.WardName,
                ward.DistrictId
            };

            var response = await _httpClient.PostAsJsonAsync("https://localhost:7096/api/wards", payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"❌ Ward Create Error: {content}");

            var created = JsonConvert.DeserializeObject<WardViewModel>(content)!;
            ward.Id = created.Id; // ✅ gán lại Id
            return created;
        }



        private async Task<IActionResult> ReloadViewAsync(AddressCreateViewModel model)
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "locations.json");
            var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(jsonContent);

            model.Cities = cities ?? new();
            ViewBag.CurrentUserName = HttpContext.Session.GetString("FullName");

            return View(model);
        }


        private async Task<List<Users>> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync("https://localhost:7096/api/users");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Users>>(json) ?? new();
        }


        // XÓA ĐỊA CHỈ
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

            if (allAddresses.Count == 1)
            {
                TempData["ToastMessage"] = "Bạn phải có ít nhất 1 địa chỉ";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Address");
            }

            if (address.Status == AddressStatus.Default)
            {
                TempData["ToastMessage"] = "Không thể xoá địa chỉ mặc định";
                TempData["ToastType"] = "warning";
                return RedirectToAction("Address");
            }

            try
            {
                // 🚨 Lấy danh sách Districts kèm Wards từ cityId (address.Id == City.Id)
                // TEMP FIX (dùng nếu /by-city fail)
                var response = await _httpClient.GetAsync("https://localhost:7096/api/districts");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Không thể lấy tất cả Districts: " + json);

                var allDistricts = JsonConvert.DeserializeObject<List<Districts>>(json);
                var districts = allDistricts.Where(d => d.CityId == id).ToList();


                // 🧹 Xoá từng Ward rồi đến District
                foreach (var district in districts)
                {
                    foreach (var ward in district.Wards ?? new List<Wards>())
                    {
                        await DeleteWardByIdAsync(ward.Id);
                    }

                    await DeleteDistrictByIdAsync(district.Id);
                }

                // 🧨 Xoá City sau cùng
                await DeleteCityAsync(address.Id);


                // ✅ Xoá địa chỉ chính
                var success = await _service.DeleteAddressAsync(id);
                TempData["ToastMessage"] = success
                    ? "Xoá địa chỉ thành công!"
                    : "Xoá địa chỉ thất bại!";
                TempData["ToastType"] = success ? "success" : "danger";
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Lỗi khi xoá địa chỉ: {ex.Message}";
                TempData["ToastType"] = "danger";
            }

            return RedirectToAction("Address");
        }

        private async Task DeleteCityAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"https://localhost:7096/api/cities/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception("Xoá City thất bại: " + msg);
            }
        }

        private async Task DeleteDistrictByIdAsync(int districtId)
        {
            var response = await _httpClient.DeleteAsync($"https://localhost:7096/api/districts/{districtId}");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception("Xoá District thất bại: " + msg);
            }
        }

        private async Task DeleteWardByIdAsync(int wardId)
        {
            var response = await _httpClient.DeleteAsync($"https://localhost:7096/api/wards/{wardId}");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception("Xoá Ward thất bại: " + msg);
            }
        }


        // EDIT ADDRESS

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var address = await _service.GetAddressByIdAsync(id);
            if (address == null)
            {
                TempData["Error"] = "Không tìm thấy địa chỉ!";
                return RedirectToAction("Address");
            }

            var parts = address.Description?.Split(',').Select(p => p.Trim()).ToArray();
            string wardName = parts?.ElementAtOrDefault(0) ?? "";
            string districtName = parts?.ElementAtOrDefault(1) ?? "";
            string cityName = parts?.ElementAtOrDefault(2) ?? "";

            var cities = await _httpClient.GetFromJsonAsync<List<CityViewModel>>("https://localhost:7096/api/cities") ?? new();
            var matchedCity = cities.FirstOrDefault(c => c.CityName.Trim().Equals(cityName, StringComparison.OrdinalIgnoreCase));

            var districts = new List<DistrictViewModel>();
            var wards = new List<WardViewModel>();
            int cityId = 0, districtId = 0, wardId = 0;

            if (matchedCity != null)
            {
                cityId = matchedCity.Id;
                var distRes = await _httpClient.GetAsync($"https://localhost:7096/api/districts/city/{cityId}");
                if (distRes.IsSuccessStatusCode)
                {
                    districts = JsonConvert.DeserializeObject<List<DistrictViewModel>>(await distRes.Content.ReadAsStringAsync()) ?? new();
                    var matchedDistrict = districts.FirstOrDefault(d => d.DistrictName.Trim().Equals(districtName, StringComparison.OrdinalIgnoreCase));
                    if (matchedDistrict != null)
                    {
                        districtId = matchedDistrict.Id;
                        var wardRes = await _httpClient.GetAsync($"https://localhost:7096/api/wards/district/{districtId}");
                        if (wardRes.IsSuccessStatusCode)
                        {
                            wards = JsonConvert.DeserializeObject<List<WardViewModel>>(await wardRes.Content.ReadAsStringAsync()) ?? new();
                            var matchedWard = wards.FirstOrDefault(w => w.WardName.Trim().Equals(wardName, StringComparison.OrdinalIgnoreCase));
                            if (matchedWard != null)
                            {
                                wardId = matchedWard.Id;
                            }
                        }
                    }
                }
            }

            var vm = new AddressEditViewModel
            {
                Id = address.Id,
                UserId = address.UserId,
                Name = address.Name,
                Phone = address.Phone,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                Description = address.Description,
                Status = address.Status,
                CityName = cityName,
                DistrictName = districtName,
                WardName = wardName,
                CityId = cityId,
                DistrictId = districtId,
                WardId = wardId,
                Cities = cities,
                Districts = districts,
                Wards = wards
            };
            ViewBag.CityId = cityId;
            ViewBag.DistrictId = districtId;
            ViewBag.WardId = wardId;

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(AddressEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ToastMessage"] = "Dữ liệu không hợp lệ!";
                TempData["ToastType"] = "danger";
                return View(model);
            }

            try
            {
                model.Name = model.Name?.Trim();
                model.Phone = model.Phone?.Trim();
                model.Description = model.Description?.Trim();

                // 📦 1. Load dữ liệu từ locations.json
                var cityModel = await SaveCityForUpdateAsync(model.CityName);
                var districtModel = await SaveDistrictForUpdateAsync(model.DistrictName, model.CityName);
                var wardModel = await SaveWardForUpdateAsync(model.WardName, model.DistrictName, model.CityName);

                // ✅ 2. CITY: Shared PK với Address → phải dùng model.Id
                var createdCity = new CityViewModel
                {
                    Id = model.Id,
                    CityName = cityModel.CityName
                };
                await SaveCityAsync(createdCity); // POST nếu chưa có, PUT nếu đã tồn tại

                // ✅ 3. DISTRICT: Dùng tên + cityId để lấy Id (GET hoặc POST → trả về Id thật)
                var savedDistrict = await SaveDistrictAsync(new DistrictViewModel
                {
                    DistrictName = districtModel.DistrictName,
                    CityId = createdCity.Id
                });

                // ✅ 4. WARD: Dùng tên + districtId để lấy Id (GET hoặc POST → trả về Id thật)
                var savedWard = await SaveWardAsync(new WardViewModel
                {
                    WardName = wardModel.WardName,
                    DistrictId = savedDistrict.Id
                });

                // ✅ 5. Gán lại Id để cập nhật vào Address
                model.CityId = createdCity.Id;
                model.DistrictId = savedDistrict.Id;
                model.WardId = savedWard.Id;

                var addressPayload = new
                {
                    model.Id,
                    model.UserId,
                    model.Name,
                    model.Phone,
                    model.Latitude,
                    model.Longitude,
                    model.Description,
                    model.Status,
                    model.CityId,
                    model.DistrictId,
                    model.WardId
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"https://localhost:7096/api/addresses/{model.Id}",
                    addressPayload);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ToastMessage"] = $"Lỗi cập nhật địa chỉ: {await response.Content.ReadAsStringAsync()}";
                    TempData["ToastType"] = "danger";
                    return View(model);
                }

                TempData["ToastMessage"] = "Cập nhật thông tin thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Address");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Lỗi hệ thống: {ex.Message}";
                TempData["ToastType"] = "danger";
                return View(model);
            }
        }



        private async Task<CityViewModel> SaveCityForUpdateAsync(string cityName)
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "locations.json");
            var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(jsonContent) ?? new();

            // So sánh nguyên gốc, không normalize
            var existing = cities.FirstOrDefault(c => c.CityName == cityName);

            if (existing == null)
                throw new Exception($"❌ Không tìm thấy tỉnh/thành '{cityName}' trong file locations.json!");

            return existing;
        }


        private async Task<DistrictViewModel> SaveDistrictForUpdateAsync(string districtName, string cityName)
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "locations.json");
            var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(jsonContent) ?? new();

            var city = cities.FirstOrDefault(c => c.CityName == cityName);
            if (city == null)
                throw new Exception($"❌ Không tìm thấy tỉnh/thành '{cityName}' trong file locations.json!");

            var index = 0;
            foreach (var d in city.Districts)
            {
                if (d.DistrictName == districtName)
                {
                    return new DistrictViewModel
                    {
                        Id = index + 1, // 👈 gán Id theo index
                        DistrictName = d.DistrictName,
                        CityId = city.Id
                    };
                }
                index++;
            }

            throw new Exception($"❌ Không tìm thấy quận/huyện '{districtName}' trong tỉnh '{cityName}'!");
        }




        private async Task<WardViewModel> SaveWardForUpdateAsync(string wardName, string districtName, string cityName)
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "locations.json");
            var jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(jsonContent) ?? new();

            var city = cities.FirstOrDefault(c => c.CityName == cityName);
            if (city == null)
                throw new Exception($"❌ Không tìm thấy tỉnh/thành '{cityName}' trong file locations.json!");

            var district = city.Districts.FirstOrDefault(d => d.DistrictName == districtName);
            if (district == null)
                throw new Exception($"❌ Không tìm thấy quận/huyện '{districtName}' trong tỉnh '{cityName}'!");

            var index = 0;
            foreach (var w in district.Wards)
            {
                if (w.WardName == wardName)
                {
                    return new WardViewModel
                    {
                        Id = index + 1, // 👈 gán Id theo index
                        WardName = w.WardName,
                        DistrictId = district.Id
                    };
                }
                index++;
            }

            throw new Exception($"❌ Không tìm thấy phường/xã '{wardName}' trong huyện '{districtName}'!");
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
                if (addr.Status == AddressStatus.Default)
                {
                    addr.Status = AddressStatus.NotDefault;
                    await _service.UpdateAddressAsync(addr);
                }
            }


            // Cập nhật địa chỉ được chọn thành "Mặc định"
            addressToSet.Status = AddressStatus.Default;
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
                foreach (var addr in allAddresses.Where(a => a.UserId == pending.UserId && a.Status == AddressStatus.Default))
                {
                    addr.Status = AddressStatus.NotDefault;
                    await _service.UpdateAddressAsync(addr);
                }
            }
            else
            {
                // ❌ Nếu chọn KHÔNG: ép status về "Không mặc định"
                pending.Status = AddressStatus.NotDefault;
            }


            var result = await _service.AddAddressAsync(pending);
            TempData["Success"] = confirm
                ? "Đã thay thế địa chỉ mặc định."
                : "Đã thêm địa chỉ mới không phải mặc định";

            return RedirectToAction("Address");
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
                foreach (var addr in allAddresses.Where(a => a.UserId == pending.UserId && a.Status == AddressStatus.Default && a.Id != pending.Id))
                {
                    addr.Status = AddressStatus.NotDefault;
                    await _service.UpdateAddressAsync(addr);
                }
            }
            else
            {
                pending.Status = AddressStatus.NotDefault;
            }

            var success = await _service.UpdateAddressAsync(pending);
            TempData["Success"] = confirm
                ? "Đã thay thế địa chỉ mặc định thành công."
                : "Đã cập nhật địa chỉ ở chế độ không mặc định.";

            return RedirectToAction("Address");
        }


    }
}