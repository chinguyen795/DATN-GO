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
        public async Task<(bool Success, Decorates Data, string Message)> CreateAsync(Decorates request)
        {
            try
            {
                // Kiểm tra tồn tại
                var existing = await GetDecorateByUserIdAsync(request.UserId);
                if (existing != null)
                {
                    Console.WriteLine($"🔁 User {request.UserId} đã có decorate → update thay vì tạo mới.");

                    // Gọi UpdateAsync thay vì Post
                    var (success, updated, msg) = await UpdateAsync(existing.Id, request);
                    return (success, updated, msg);
                }

                // Xử lý upload base64 cho các trường ảnh và video
                var uploadMap = new Dictionary<string, string>
                {
                    { "Video", "decorates/videos" },
                    { "Slide1", "decorates/slideshow" },
                    { "Slide2", "decorates/slideshow" },
                    { "Slide3", "decorates/slideshow" },
                    { "Slide4", "decorates/slideshow" },
                    { "Slide5", "decorates/slideshow" },
                    { "Image1", "decorates/images" },
                    { "Image2", "decorates/images" }
                };

                // Xử lý các file base64 và lưu lên server
                foreach (var item in uploadMap)
                {
                    var prop = typeof(Decorates).GetProperty(item.Key);
                    var base64 = prop?.GetValue(request) as string;

                    if (!string.IsNullOrWhiteSpace(base64) && base64.StartsWith("data:"))
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

                // Gửi POST nếu chưa có decorate
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiUrl = $"{_baseUrl.TrimEnd('/')}/Decorates";

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return (false, null, $"❌ API lỗi: {response.StatusCode} - {responseBody}");

                var decorate = JsonConvert.DeserializeObject<Decorates>(responseBody);
                return (true, decorate, "✅ Tạo decorate thành công!");
            }
            catch (Exception ex)
            {
                return (false, null, $"🔥 Lỗi tạo decorate: {ex.Message}");
            }
        }

        // Cập nhật decorate
        public async Task<(bool Success, Decorates? Data, string Message)> UpdateAsync(int id, Decorates request)
        {
            try
            {
                // 🧠 Ensure ID trong body khớp với ID path
                request.Id = id;

                var uploadMap = new Dictionary<string, string>
                {
                    { "Video", "decorates/videos" },
                    { "Slide1", "decorates/slideshow" },
                    { "Slide2", "decorates/slideshow" },
                    { "Slide3", "decorates/slideshow" },
                    { "Slide4", "decorates/slideshow" },
                    { "Slide5", "decorates/slideshow" },
                    { "Image1", "decorates/images" },
                    { "Image2", "decorates/images" }
                };


                Console.WriteLine("🛠️ Đang xử lý upload base64 trong Update...");

                foreach (var item in uploadMap)
                {
                    var prop = typeof(Decorates).GetProperty(item.Key);
                    var base64 = prop?.GetValue(request) as string;

                    if (!string.IsNullOrWhiteSpace(base64) && base64.StartsWith("data:"))
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

                // 🔧 Chuẩn bị gọi API PUT
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl.TrimEnd('/')}/Decorates/{id}";
                Console.WriteLine($"🔄 PUT tới {url}");
                Console.WriteLine("📤 Payload gửi lên:");
                Console.WriteLine(json);

                var response = await _httpClient.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API PUT lỗi: {response.StatusCode} - {responseBody}");
                    return (false, null, $"❌ API PUT lỗi: {response.StatusCode} - {responseBody}");
                }

                // ✅ Không có body trả về thì return request luôn
                return (true, request, "🔁 Cập nhật thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception trong UpdateAsync:");
                Console.WriteLine(ex.ToString());
                return (false, null, "🔥 Lỗi UpdateAsync: " + ex.Message);
            }
        }

        // Lấy thông tin decorate của user theo UserId
        public async Task<Decorates?> GetDecorateByUserIdAsync(int userId)
        {
            try
            {
                var apiUrl = $"{_baseUrl}Decorates/user/{userId}";
                Console.WriteLine($"🌐 Gửi GET đến: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);

                var rawJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📥 JSON nhận được: {rawJson}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ API lỗi: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                var decorate = JsonConvert.DeserializeObject<Decorates>(rawJson);

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
            catch (JsonException jsonEx)
            {
                Console.WriteLine("🔥 JSON deserialize lỗi:");
                Console.WriteLine(jsonEx.Message);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 Exception khác trong GetDecorateByUserIdAsync:");
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