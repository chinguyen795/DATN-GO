using DATN_GO.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace DATN_GO.Service
{
    public class DecoratesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public DecoratesService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        // ✅ Convert base64 -> file, lưu vào wwwroot + return path
        public async Task<string> UploadFileAsync(string fileData, string directory)
        {
            if (string.IsNullOrWhiteSpace(fileData))
            {
                Console.WriteLine("⚠️ fileData is null or empty");
                return null;
            }

            try
            {
                var match = Regex.Match(fileData, @"^(data:(?<type>.+?);base64,)?(?<data>.+)$");
                if (!match.Success)
                {
                    Console.WriteLine("❌ Base64 format mismatch");
                    return null;
                }

                string contentType = match.Groups["type"].Value;
                string base64 = match.Groups["data"].Value;

                if (string.IsNullOrEmpty(base64))
                {
                    Console.WriteLine("❌ Base64 data is empty!");
                    return null;
                }

                string ext = string.IsNullOrEmpty(contentType)
                    ? "jpg"
                    : contentType.Split('/').Last().ToLower();

                var allowedExts = new[] { "jpg", "jpeg", "png", "gif", "mp4", "avi", "mov", "webp" };
                if (!allowedExts.Contains(ext))
                {
                    Console.WriteLine($"❌ Invalid extension: {ext}");
                    return null;
                }

                var fileName = $"{Guid.NewGuid():N}.{ext}";
                var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", directory);
                var savePath = Path.Combine(saveDir, fileName);
                Directory.CreateDirectory(saveDir);

                byte[] fileBytes = Convert.FromBase64String(base64);
                await File.WriteAllBytesAsync(savePath, fileBytes);

                return $"/{directory}/{fileName}";
            }
            catch (FormatException ex)
            {
                Console.WriteLine("🔥 Base64 Format Exception:");
                Console.WriteLine(fileData.Substring(0, Math.Min(100, fileData.Length)));
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception writing file:");
                Console.WriteLine(ex);
            }

            return null;
        }

        // ✅ Upload nhiều file base64
        public async Task<List<string>> UploadMultipleFilesAsync(List<string> fileDataList, string directory)
        {
            var uploadedFiles = new List<string>();

            if (fileDataList == null || !fileDataList.Any())
            {
                Console.WriteLine("⚠️ Empty file list");
                return uploadedFiles;
            }

            foreach (var fileData in fileDataList)
            {
                var uploadedFile = await UploadFileAsync(fileData, directory);
                if (!string.IsNullOrEmpty(uploadedFile))
                    uploadedFiles.Add(uploadedFile);
            }

            return uploadedFiles;
        }

        // Tạo decorate
        public async Task<(bool Success, Decorates? Data, string Message)> CreateAsync(Decorates request)
        {
            try
            {
                // 1) Kiểm tra tồn tại global
                var existing = await GetGlobalDecorateAsync();
                if (existing != null)
                {
                    Console.WriteLine("🔁 Đã có decorate global → update thay vì tạo mới.");
                    var (success, updated, msg) = await UpdateAsync(existing.Id, request);
                    return (success, updated, msg);
                }

                // 2) Xử lý upload base64 cho ảnh/video
                var uploadMap = new Dictionary<string, string>
        {
            { "Video",  "decorates/videos" },
            { "Slide1", "decorates/slideshow" },
            { "Slide2", "decorates/slideshow" },
            { "Slide3", "decorates/slideshow" },
            { "Slide4", "decorates/slideshow" },
            { "Slide5", "decorates/slideshow" },
            { "Image1", "decorates/images" },
            { "Image2", "decorates/images" }
        };

                foreach (var item in uploadMap)
                {
                    var prop = typeof(Decorates).GetProperty(item.Key);
                    var base64 = prop?.GetValue(request) as string;

                    if (!string.IsNullOrWhiteSpace(base64) && base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        var uploadedUrl = await UploadFileAsync(base64, item.Value);
                        if (string.IsNullOrEmpty(uploadedUrl))
                        {
                            Console.WriteLine($"❌ Upload thất bại: {item.Key}");
                            return (false, null, $"❌ Upload {item.Key} thất bại!");
                        }

                        prop.SetValue(request, uploadedUrl);
                    }
                }

                // 3) POST tạo mới
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var apiUrl = $"{_baseUrl.TrimEnd('/')}/Decorates";

                Console.WriteLine($"📤 POST tới {apiUrl}");
                Console.WriteLine(json);

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API lỗi: {response.StatusCode} - {responseBody}");
                    return (false, null, $"❌ API lỗi: {response.StatusCode} - {responseBody}");
                }

                var decorate = JsonConvert.DeserializeObject<Decorates>(responseBody);
                return (true, decorate, "✅ Tạo decorate thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception trong CreateAsync:");
                Console.WriteLine(ex.ToString());
                return (false, null, $"🔥 Lỗi tạo decorate: {ex.Message}");
            }
        }

        // Cập nhật decorate
        public async Task<(bool Success, Decorates? Data, string Message)> UpdateAsync(int id, Decorates request)
        {
            try
            {
                // 0) Nếu id không hợp lệ → lấy global decorate để cập nhật
                if (id <= 0)
                {
                    var currentGlobal = await GetGlobalDecorateAsync();
                    if (currentGlobal == null)
                        return (false, null, "❌ Không tìm thấy decorate global để cập nhật.");
                    id = currentGlobal.Id;
                }

                // 1) Lấy bản hiện tại (để merge tránh ghi đè null)
                var urlGet = $"{_baseUrl.TrimEnd('/')}/Decorates/{id}";
                var getRes = await _httpClient.GetAsync(urlGet);
                if (!getRes.IsSuccessStatusCode)
                    return (false, null, $"❌ Không lấy được decorate hiện tại (HTTP {(int)getRes.StatusCode}).");

                var currentJson = await getRes.Content.ReadAsStringAsync();
                var current = JsonConvert.DeserializeObject<Decorates>(currentJson);
                if (current == null)
                    return (false, null, "❌ JSON decorate hiện tại không hợp lệ.");

                // 2) Chuẩn hoá ID body
                request.Id = id;

                // 3) Upload base64 nếu có
                var uploadMap = new Dictionary<string, string>
        {
            { "Video",  "decorates/videos" },
            { "Slide1", "decorates/slideshow" },
            { "Slide2", "decorates/slideshow" },
            { "Slide3", "decorates/slideshow" },
            { "Slide4", "decorates/slideshow" },
            { "Slide5", "decorates/slideshow" },
            { "Image1", "decorates/images" },
            { "Image2", "decorates/images" }
        };

                Console.WriteLine("🛠️ Đang xử lý upload base64 trong UpdateAsync...");
                foreach (var kv in uploadMap)
                {
                    var prop = typeof(Decorates).GetProperty(kv.Key);
                    if (prop == null) continue;

                    var val = prop.GetValue(request) as string;
                    if (!string.IsNullOrWhiteSpace(val) && val.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        var uploadedUrl = await UploadFileAsync(val, kv.Value);
                        if (string.IsNullOrEmpty(uploadedUrl))
                            return (false, null, $"❌ Upload {kv.Key} thất bại!");

                        prop.SetValue(request, uploadedUrl);
                    }
                }

                // 4) Merge: nếu field string trong request == null → giữ nguyên từ current
                //    (nếu bạn muốn cho phép xóa giá trị, gửi chuỗi rỗng "" thay vì null)
                foreach (var p in typeof(Decorates).GetProperties())
                {
                    if (p.PropertyType == typeof(string))
                    {
                        var newVal = p.GetValue(request) as string;
                        if (newVal == null)
                        {
                            var oldVal = p.GetValue(current) as string;
                            p.SetValue(request, oldVal);
                        }
                    }
                }

                // 5) PUT cập nhật
                var urlPut = $"{_baseUrl.TrimEnd('/')}/Decorates/{id}";
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔄 PUT tới {urlPut}");
                Console.WriteLine("📤 Payload gửi lên:");
                Console.WriteLine(json);

                var response = await _httpClient.PutAsync(urlPut, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API PUT lỗi: {response.StatusCode} - {responseBody}");
                    return (false, null, $"❌ API PUT lỗi: {response.StatusCode} - {responseBody}");
                }

                // 6) Thử parse body trả về; nếu rỗng thì trả request đã merge
                Decorates? updated = null;
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        updated = JsonConvert.DeserializeObject<Decorates>(responseBody);
                    }
                    catch
                    {
                        // bỏ qua, dùng request thay thế
                    }
                }

                return (true, updated ?? request, "🔁 Cập nhật thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception trong UpdateAsync:");
                Console.WriteLine(ex);
                return (false, null, "🔥 Lỗi UpdateAsync: " + ex.Message);
            }
        }


        // ✅ Lấy thông tin decorate GLOBAL
        public async Task<Decorates?> GetGlobalDecorateAsync()
        {
            try
            {
                var apiUrl = $"{_baseUrl.TrimEnd('/')}/Decorates";
                Console.WriteLine($"🌐 GET: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                var rawJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📥 JSON: {rawJson}");

                if (!response.IsSuccessStatusCode) return null;

                var list = JsonConvert.DeserializeObject<List<Decorates>>(rawJson);
                if (list == null || list.Count == 0) return null;

                // tuỳ bạn: lấy bản ghi mới nhất theo Id
                var decorate = list.OrderByDescending(x => x.Id).FirstOrDefault();
                Console.WriteLine("✅ Deserialize OK (list) → lấy 1 bản ghi.");
                return decorate;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 GetGlobalDecorateAsync error:");
                Console.WriteLine(ex);
                return null;
            }
        }



        // Lấy danh sách tất cả decorate
        public async Task<List<Decorates>> GetDecoratesAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/Decorates";
                Console.WriteLine($"🌍 Gửi GET tới: {url}");

                var response = await _httpClient.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine("📥 API Response Body:");
                Console.WriteLine(responseBody);

                if (!response.IsSuccessStatusCode)
                    return new List<Decorates>();

                return JsonConvert.DeserializeObject<List<Decorates>>(responseBody) ?? new List<Decorates>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception khi gọi API GetDecorates:");
                Console.WriteLine(ex.ToString());
                return new List<Decorates>();
            }
        }

        // Xóa tất cả
        public async Task<bool> DeleteDecorateAsync(int id)
        {
            try
            {
                var url = $"{_baseUrl.TrimEnd('/')}/Decorates/{id}";
                Console.WriteLine($"🗑️ Gửi DELETE đến: {url}");

                var response = await _httpClient.DeleteAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Lỗi xoá trang trí: {response.StatusCode} - {responseBody}");
                    return false;
                }

                Console.WriteLine("✅ Xoá trang trí thành công.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Lỗi trong DeleteDecorateAsync:");
                Console.WriteLine(ex.ToString());
                return false;
            }
        }






        // Lấy User theo UserId
        public async Task<Users?> GetUserByIdAsync(int userId)
        {
            try
            {
                var apiUrl = $"{_baseUrl}Users/{userId}";
                Console.WriteLine($"🌐 Gửi GET đến: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                var rawJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 JSON nhận được: {rawJson}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API lỗi: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                var user = JsonConvert.DeserializeObject<Users>(rawJson);

                if (user == null)
                {
                    Console.WriteLine("⚠️ Deserialize trả về null! Có thể JSON không khớp model.");
                }
                else
                {
                    Console.WriteLine("✅ Deserialize User thành công.");
                }

                return user;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine("🔥 JSON deserialize lỗi:");
                Console.WriteLine(jsonEx.Message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception khác trong GetUserByIdAsync:");
                Console.WriteLine(ex);
                return null;
            }
        }

        // XÓA
        public async Task<Decorates?> GetDecorateByIdAsync(int id)
        {
            try
            {
                // Lấy BaseUrl từ cấu hình (đã có sẵn trong constructor)
                var url = $"{_baseUrl}Decorates/{id}";  // Đảm bảo URL chính xác theo API của bạn
                Console.WriteLine($"🌐 Gửi GET đến: {url}");

                // Gửi yêu cầu GET đến API để lấy thông tin decorate theo ID
                var response = await _httpClient.GetAsync(url);

                // Đọc nội dung phản hồi
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API lỗi: {(int)response.StatusCode} - {responseBody}");
                    return null;
                }

                // Deserialize JSON thành đối tượng Decorates
                var decorate = JsonConvert.DeserializeObject<Decorates>(responseBody);

                if (decorate == null)
                {
                    Console.WriteLine("⚠️ Deserialize trả về null! Có thể JSON không khớp model.");
                }
                else
                {
                    Console.WriteLine("✅ Deserialize thành công.");
                }

                return decorate;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Lỗi khi gọi API GetDecorateByIdAsync:");
                Console.WriteLine(ex.ToString());
                return null;
            }
        }


        // Xóa Slide
        public async Task<bool> DeleteAllSlidesAsync(int decorateId)
        {
            try
            {
                var url = $"{_baseUrl.TrimEnd('/')}/Decorates/{decorateId}/clear-slides";
                Console.WriteLine($"🗑️ Gửi PATCH đến: {url}");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Lỗi xoá slide: {response.StatusCode}");
                    return false;
                }

                Console.WriteLine("Đã xoá toàn bộ slide!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Lỗi trong DeleteAllSlidesAsync: {ex.Message}");
                return false;
            }
        }






        // Xóa Video
        public async Task<bool> DeleteVideoAsync(int decorateId)
        {
            try
            {
                var url = $"{_baseUrl.TrimEnd('/')}/Decorates/{decorateId}/clear-video";
                Console.WriteLine($"🗑️ Gửi PATCH đến: {url}");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Lỗi xoá video: {response.StatusCode}");
                    return false;
                }

                Console.WriteLine("Đã xoá video!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi trong DeleteVideoAsync: {ex.Message}");
                return false;
            }
        }
        // Xóa ảnh 1
        public async Task<bool> DeleteDecorate1Async(int decorateId)
        {
            try
            {
                var url = $"{_baseUrl.TrimEnd('/')}/Decorates/{decorateId}/clear-decorate1";
                Console.WriteLine($"🗑️ Gửi PATCH đến: {url}");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Lỗi xoá ảnh 1: {response.StatusCode}");
                    return false;
                }
                Console.WriteLine("Đã xoá ảnh 1!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi DeleteDecorate1Async: {ex.Message}");
                return false;
            }
        }

        // Xóa ảnh 2
        public async Task<bool> DeleteDecorate2Async(int decorateId)
        {
            try
            {
                var url = $"{_baseUrl.TrimEnd('/')}/Decorates/{decorateId}/clear-decorate2";
                Console.WriteLine($"🗑️ Gửi PATCH đến: {url}");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Lỗi xoá ảnh 2: {response.StatusCode}");
                    return false;
                }
                Console.WriteLine("Đã xoá ảnh 2!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi DeleteDecorate2Async: {ex.Message}");
                return false;
            }
        }

    }
}