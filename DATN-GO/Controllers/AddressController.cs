using DATN_GO.Models;
using DATN_GO.Services;
using DATN_GO.ViewModels.Address;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
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
            // Check login
            var userIdStr = HttpContext.Session.GetString("Id");
            if (string.IsNullOrEmpty(userIdStr))
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                TempData["ToastType"] = "warning";
                TempData["TriggerLoginModal"] = true;
                return RedirectToAction("Index", "Home");
            }
            int.TryParse(userIdStr, out var currentUserId);

            // Lấy addresses của user (lọc bớt auto create)
            var addresses = await _service.GetAddressesAsync();
            var userAddresses = addresses
                .Where(a => a.UserId == currentUserId &&
                            (a.Description == null || !a.Description.StartsWith("Tự động tạo cho")))
                .ToList();

            // Gọi 3 API song song cho nhanh
            var wardsTask = _httpClient.GetAsync("https://localhost:7096/api/wards");
            var districtsTask = _httpClient.GetAsync("https://localhost:7096/api/districts");
            var citiesTask = _httpClient.GetAsync("https://localhost:7096/api/cities");

            await Task.WhenAll(wardsTask, districtsTask, citiesTask);

            var wardRes = wardsTask.Result;
            var districtRes = districtsTask.Result;
            var cityRes = citiesTask.Result;

            if (!wardRes.IsSuccessStatusCode || !districtRes.IsSuccessStatusCode || !cityRes.IsSuccessStatusCode)
            {
                // Không có dữ liệu location ⇒ vẫn show danh sách address thô (đỡ trắng trang)
                var fallback = userAddresses.Select(addr => new AddressViewModel
                {
                    Id = addr.Id,
                    UserId = addr.UserId,
                    Name = addr.Name ?? string.Empty,
                    Phone = addr.Phone ?? string.Empty,
                    Latitude = addr.Latitude,
                    Longitude = addr.Longitude,
                    Description = addr.Description,
                    Status = addr.Status,
                    CityName = null,
                    DistrictName = null,
                    WardName = null
                })
                // sắp xếp: mặc định lên đầu cho đẹp giống view
                .OrderByDescending(a => a.Status == AddressStatus.Default)
                .ToList();

                return View(fallback);
            }

            // Parse JSON
            var wards = JsonConvert.DeserializeObject<List<WardViewModel>>(await wardRes.Content.ReadAsStringAsync()) ?? new();
            var districts = JsonConvert.DeserializeObject<List<DistrictViewModel>>(await districtRes.Content.ReadAsStringAsync()) ?? new();
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(await cityRes.Content.ReadAsStringAsync()) ?? new();

            // Dictionnaries cho lookup O(1)
            var wardsById = wards.GroupBy(w => w.Id).ToDictionary(g => g.Key, g => g.First());
            var districtsById = districts.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
            var citiesById = cities.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());

            // Map chuẩn: WardId -> District -> City (fallback DistrictId nếu WardId null)
            var vmList = new List<AddressViewModel>();
            foreach (var addr in userAddresses)
            {
                WardViewModel? ward = null;
                DistrictViewModel? district = null;
                CityViewModel? city = null;

                // Ưu tiên WardId
                if (addr.WardId.HasValue && wardsById.TryGetValue(addr.WardId.Value, out var w))
                {
                    ward = w;
                    // Ward biết DistrictId
                    if (districtsById.TryGetValue(ward.DistrictId, out var dFromWard))
                        district = dFromWard;
                }

                // Fallback: nếu chưa có district mà Address có DistrictId
                if (district == null && addr.DistrictId.HasValue)
                {
                    districtsById.TryGetValue(addr.DistrictId.Value, out district);
                }

                // City lấy từ district.CityId
                if (district != null)
                {
                    citiesById.TryGetValue(district.CityId, out city);
                }

                vmList.Add(new AddressViewModel
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
                    WardName = ward?.WardName,

                    // Nếu view có xài Id thì fill luôn (optional)
                    CityId = city?.Id ?? 0,
                    DistrictId = district?.Id ?? addr.DistrictId ?? 0,
                    WardId = ward?.Id ?? addr.WardId ?? 0
                });
            }

            // Mặc định lên đầu
            vmList = vmList.OrderByDescending(a => a.Status == AddressStatus.Default).ToList();

            return View(vmList);
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

            // ✅ Validate số điện thoại Việt Nam
            var vnPhoneRegex = new System.Text.RegularExpressions.Regex(@"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-689]|9\d)\d{7}$");
            if (string.IsNullOrWhiteSpace(model.Phone) || !vnPhoneRegex.IsMatch(model.Phone))
            {
                TempData["ToastMessage"] = "Số điện thoại không hợp lệ. Vui lòng nhập đúng số điện thoại Việt Nam.";
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

            // Nếu chọn làm mặc định, gỡ mặc định cũ
            if (model.Status == AddressStatus.Default)
            {
                var existingDefault = userAddresses.FirstOrDefault(a => a.Status == AddressStatus.Default);
                if (existingDefault != null)
                {
                    existingDefault.Status = AddressStatus.NotDefault;
                    await _service.UpdateAddressAsync(existingDefault);
                }
            }

            // 1) Tạo Address trước (chưa có DistrictId/WardId)
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

            // 2) Tạo/đồng bộ City → District → Ward
            try
            {
                // City dùng shared PK = Address.Id
                var cityModel = new CityViewModel
                {
                    Id = newAddressId,
                    CityName = model.CityName?.Trim() ?? string.Empty
                };
                await SaveCityAsync(cityModel);

                var districtModel = new DistrictViewModel
                {
                    CityId = cityModel.Id, // = newAddressId
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
                var createdWard = await SaveWardAsync(wardModel);

                // 3) ✅ Cập nhật lại Address với DistrictId/WardId
                address.Id = newAddressId;
                address.DistrictId = createdDistrict.Id;
                address.WardId = createdWard.Id;
                await _service.UpdateAddressAsync(address);
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


        // XOÁ ĐỊA CHỈ
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
                // ✅ 1) XÓA ADDRESS TRƯỚC (cắt FK tới Ward)
                var deleted = await _service.DeleteAddressAsync(id);
                if (!deleted)
                {
                    TempData["ToastMessage"] = "Xoá địa chỉ thất bại!";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Address");
                }

                // ✅ 2) LẤY DISTRICTS THEO CITY (City.Id = Address.Id theo shared PK)
                var districtsRes = await _httpClient.GetAsync("https://localhost:7096/api/districts");
                if (!districtsRes.IsSuccessStatusCode)
                {
                    var msg = await districtsRes.Content.ReadAsStringAsync();
                    throw new Exception("Không thể lấy Districts: " + msg);
                }
                var allDistricts = JsonConvert.DeserializeObject<List<Districts>>(await districtsRes.Content.ReadAsStringAsync()) ?? new();
                var districts = allDistricts.Where(d => d.CityId == id).ToList();
                var districtIds = districts.Select(d => d.Id).ToList();

                // ✅ 3) LẤY TẤT CẢ WARDS RỒI FILTER THEO DISTRICTID
                var wardsRes = await _httpClient.GetAsync("https://localhost:7096/api/wards");
                if (!wardsRes.IsSuccessStatusCode)
                {
                    var msg = await wardsRes.Content.ReadAsStringAsync();
                    throw new Exception("Không thể lấy Wards: " + msg);
                }
                var allWards = JsonConvert.DeserializeObject<List<Wards>>(await wardsRes.Content.ReadAsStringAsync()) ?? new();
                var wardsToDelete = allWards.Where(w => districtIds.Contains(w.DistrictId)).ToList();

                // ✅ 4) XOÁ WARD TRƯỚC
                foreach (var w in wardsToDelete)
                {
                    await DeleteWardByIdAsync(w.Id);
                }

                // ✅ 5) XOÁ DISTRICT
                foreach (var d in districts)
                {
                    await DeleteDistrictByIdAsync(d.Id);
                }

                // ✅ 6) XOÁ CITY SAU CÙNG
                await DeleteCityAsync(id);

                TempData["ToastMessage"] = "Xoá địa chỉ thành công!";
                TempData["ToastType"] = "success";
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
            var response = await _httpClient.GetAsync($"https://localhost:7096/api/cities/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound) return; // City đã bị xoá
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception("Xoá City thất bại: " + msg);
            }

            // City còn tồn tại → xoá
            var delRes = await _httpClient.DeleteAsync($"https://localhost:7096/api/cities/{id}");
            if (!delRes.IsSuccessStatusCode)
            {
                var msg = await delRes.Content.ReadAsStringAsync();
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

            // gọi 3 API location một lượt
            var wardTask = _httpClient.GetAsync("https://localhost:7096/api/wards");
            var districtTask = _httpClient.GetAsync("https://localhost:7096/api/districts");
            var cityTask = _httpClient.GetAsync("https://localhost:7096/api/cities");

            await Task.WhenAll(wardTask, districtTask, cityTask);

            if (!wardTask.Result.IsSuccessStatusCode ||
                !districtTask.Result.IsSuccessStatusCode ||
                !cityTask.Result.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tải được dữ liệu khu vực.";
                return RedirectToAction("Address");
            }

            var wards = JsonConvert.DeserializeObject<List<WardViewModel>>(await wardTask.Result.Content.ReadAsStringAsync()) ?? new();
            var districts = JsonConvert.DeserializeObject<List<DistrictViewModel>>(await districtTask.Result.Content.ReadAsStringAsync()) ?? new();
            var cities = JsonConvert.DeserializeObject<List<CityViewModel>>(await cityTask.Result.Content.ReadAsStringAsync()) ?? new();

            // Lấy theo ID thật:
            // City: shared PK = Address.Id
            var city = cities.FirstOrDefault(c => c.Id == address.Id);
            var cityName = city?.CityName?.Trim() ?? string.Empty;

            // District/Ward: theo Address.DistrictId / WardId lưu trong Address
            var district = (address.DistrictId.HasValue && address.DistrictId > 0)
                ? districts.FirstOrDefault(d => d.Id == address.DistrictId.Value)
                : null;
            var districtName = district?.DistrictName?.Trim() ?? string.Empty;

            var ward = (address.WardId.HasValue && address.WardId > 0)
                ? wards.FirstOrDefault(w => w.Id == address.WardId.Value)
                : null;
            var wardName = ward?.WardName?.Trim() ?? string.Empty;

            // Truyền qua ViewBag để JS set selected theo TÊN (nhớ serialize JSON ở view như mình hướng dẫn)
            ViewBag.SelectedCityName = cityName;
            ViewBag.SelectedDistrictName = districtName;
            ViewBag.SelectedWardName = wardName;

            // ViewModel cho form
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

                // bind lại ID thật (nếu cần submit)
                CityId = address.Id,                         // shared PK
                DistrictId = address.DistrictId ?? 0,
                WardId = address.WardId ?? 0
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(AddressEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // 0) Clean input
                model.Name = model.Name?.Trim();
                model.Phone = model.Phone?.Trim();
                model.Description = model.Description?.Trim();
                model.CityName = (model.CityName ?? "").Trim();
                model.DistrictName = (model.DistrictName ?? "").Trim();
                model.WardName = (model.WardName ?? "").Trim();

                if (string.IsNullOrWhiteSpace(model.CityName) ||
                    string.IsNullOrWhiteSpace(model.DistrictName) ||
                    string.IsNullOrWhiteSpace(model.WardName))
                {
                    TempData["ToastMessage"] = "Thiếu Tỉnh/Quận/Phường.";
                    TempData["ToastType"] = "danger";
                    return View(model);
                }

                // ✅ Validate số điện thoại Việt Nam
                var vnPhoneRegex = new System.Text.RegularExpressions.Regex(
                    @"^(0|\+84)(3[2-9]|5[2689]|7[06-9]|8[1-689]|9\d)\d{7}$"
                );
                if (string.IsNullOrWhiteSpace(model.Phone) || !vnPhoneRegex.IsMatch(model.Phone))
                {
                    TempData["ToastMessage"] = "Số điện thoại không hợp lệ. Vui lòng nhập đúng số điện thoại Việt Nam.";
                    TempData["ToastType"] = "danger";
                    return View(model);
                }

                // 1) CITY: shared PK = Address.Id (PUT nếu có, POST nếu chưa có)
                await SaveCityAsync(new CityViewModel
                {
                    Id = model.Id,
                    CityName = model.CityName
                });

                // 2) DISTRICT: theo (CityId = Address.Id, DistrictName) → LẤY/CREATE → Id thật
                var districtVm = new DistrictViewModel
                {
                    CityId = model.Id, // City shared PK = Address.Id
                    DistrictName = model.DistrictName
                };
                var savedDistrict = await SaveDistrictForUpdateAsync(districtVm);
                if (savedDistrict == null || savedDistrict.Id <= 0)
                    throw new Exception("Không lấy được DistrictId hợp lệ.");

                // 3) WARD: theo (DistrictId, WardName) → LẤY/CREATE → Id thật
                var wardVm = new WardViewModel
                {
                    DistrictId = savedDistrict.Id,
                    WardName = model.WardName
                };
                var savedWard = await SaveWardForUpdateAsync(wardVm);
                if (savedWard == null || savedWard.Id <= 0)
                    throw new Exception("Không lấy được WardId hợp lệ.");

                // 4) Gán lại Id để binding
                model.CityId = model.Id;
                model.DistrictId = savedDistrict.Id;
                model.WardId = savedWard.Id;

                // 5) PUT Address — API server đã lưu DistrictId/WardId
                var payload = new
                {
                    model.Id,
                    model.UserId,
                    model.Name,
                    model.Phone,
                    model.Latitude,
                    model.Longitude,
                    model.Description,
                    model.Status,
                    model.DistrictId,
                    model.WardId
                };

                var res = await _httpClient.PutAsJsonAsync($"https://localhost:7096/api/addresses/{model.Id}", payload);
                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    TempData["ToastMessage"] = $"Lỗi cập nhật địa chỉ: {body}";
                    TempData["ToastType"] = "danger";
                    return View(model);
                }

                TempData["ToastMessage"] = "Cập nhật địa chỉ thành công!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Address");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"💥 Lỗi hệ thống: {ex.Message}";
                TempData["ToastType"] = "danger";
                return View(model);
            }
        }



        private async Task<DistrictViewModel?> SaveDistrictForUpdateAsync(DistrictViewModel district)
        {
            var checkUrl = $"https://localhost:7096/api/districts/by-name?name={Uri.EscapeDataString(district.DistrictName)}&cityId={district.CityId}";
            var check = await _httpClient.GetAsync(checkUrl);

            if (check.IsSuccessStatusCode)
            {
                var json = await check.Content.ReadAsStringAsync();
                var existing = JsonConvert.DeserializeObject<DistrictViewModel>(json);
                if (existing != null && existing.Id > 0)
                {
                    district.Id = existing.Id;
                    return existing;
                }
            }

            var payload = new { district.DistrictName, district.CityId };
            var response = await _httpClient.PostAsJsonAsync("https://localhost:7096/api/districts", payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"❌ District Create Error: {content}");

            var created = JsonConvert.DeserializeObject<DistrictViewModel>(content);
            if (created == null || created.Id <= 0)
                throw new Exception("❌ District Create Error: invalid response Id");

            district.Id = created.Id;
            return created;
        }

        private async Task<WardViewModel?> SaveWardForUpdateAsync(WardViewModel ward)
        {
            var checkUrl = $"https://localhost:7096/api/wards/by-name?name={Uri.EscapeDataString(ward.WardName)}&districtId={ward.DistrictId}";
            var check = await _httpClient.GetAsync(checkUrl);

            if (check.IsSuccessStatusCode)
            {
                var json = await check.Content.ReadAsStringAsync();
                var existing = JsonConvert.DeserializeObject<WardViewModel>(json);
                if (existing != null && existing.Id > 0)
                {
                    ward.Id = existing.Id;
                    return existing;
                }
            }

            var payload = new { ward.WardName, ward.DistrictId };
            var response = await _httpClient.PostAsJsonAsync("https://localhost:7096/api/wards", payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"❌ Ward Create Error: {content}");

            var created = JsonConvert.DeserializeObject<WardViewModel>(content);
            if (created == null || created.Id <= 0)
                throw new Exception("❌ Ward Create Error: invalid response Id");

            ward.Id = created.Id;
            return created;
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