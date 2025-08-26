using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using DATN_GO.Models;
using DATN_GO.Services;
using DATN_GO.ViewModels;

namespace DATN_GO.Services
{
    public class CategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly GoogleCloudStorageService _gcsService;

        public CategoryService(HttpClient httpClient, IConfiguration configuration, GoogleCloudStorageService gcsService)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
            _gcsService = gcsService;
        }

        // GET: Lấy tất cả categories
        public async Task<(bool Success, List<Categories> Data, string Message)> GetAllCategoriesAsync()
        {
            try
            {
            var response = await _httpClient.GetAsync($"{_baseUrl}Categories");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                    var categories = JsonSerializer.Deserialize<List<Categories>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (true, categories ?? new List<Categories>(), "Lấy danh sách thành công!");
            }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return (false, new List<Categories>(), errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, new List<Categories>(), $"Lỗi kết nối: {ex.Message}");
            }
        }

        // GET: Lấy category theo ID
        public async Task<(bool Success, Categories Data, string Message)> GetCategoryByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Categories/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var category = JsonSerializer.Deserialize<Categories>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (true, category, "Lấy dữ liệu thành công!");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, null, "Không tìm thấy danh mục!");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return (false, null, errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi kết nối: {ex.Message}");
            }
        }

        // POST: Tạo category mới
        public async Task<(bool Success, Categories Data, string Message)> CreateCategoryAsync(CreateCategoryRequest request)
        {
            try
            {
                // Xử lý upload hình ảnh nếu có
                if (!string.IsNullOrEmpty(request.Image))
                {
                    var imageUrl = await UploadImageAsync(request.Image);
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return (false, null, "Upload hình ảnh thất bại!");
                    }
                    request.Image = imageUrl; // Thay base64 bằng URL
                }

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Categories", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var category = JsonSerializer.Deserialize<Categories>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return (true, category, "Tạo danh mục thành công!");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return (false, null, errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi kết nối: {ex.Message}");
            }
        }

        // PUT: Cập nhật category
        public async Task<(bool Success, string Message)> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
        {
            try
            {
                // Đảm bảo ID trong request khớp với ID trong URL
                request.Id = id;

                // Xử lý upload hình ảnh nếu có và là base64
                if (!string.IsNullOrEmpty(request.Image) && IsBase64String(request.Image))
                {
                    var imageUrl = await UploadImageAsync(request.Image);
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        return (false, "Upload hình ảnh thất bại!");
                    }
                    request.Image = imageUrl; // Thay base64 bằng URL
        }

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}Categories/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cập nhật danh mục thành công!");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "Không tìm thấy danh mục!");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return (false, errorMessage);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }

        // DELETE: Xóa category
        public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
        {
            try
        {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}Categories/{id}");

            if (response.IsSuccessStatusCode)
            {
                    return (true, "Xóa danh mục thành công!");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "Không tìm thấy danh mục!");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return (false, errorMessage);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
            }

        // Phương thức upload hình ảnh lên Google Cloud Storage
        private async Task<string> UploadImageAsync(string base64Image)
        {
            try
            {
                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}.jpg";

                // Xác định folder cho category images
                var folder = "categories/";

                // Convert base64 thành IFormFile
                var formFile = ConvertBase64ToFormFile(base64Image, fileName);

                // Upload lên Google Cloud Storage
                var imageUrl = await _gcsService.UploadFileAsync(formFile, folder);

                return imageUrl;
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                Console.WriteLine($"Upload image error: {ex.Message}");
            return null;
        }
        }

        // Convert base64 string thành IFormFile
        private IFormFile ConvertBase64ToFormFile(string base64String, string fileName)
        {
            var base64Parts = base64String.Split(',');
            var base64Data = base64Parts.Length > 1 ? base64Parts[1] : base64Parts[0];
            var bytes = Convert.FromBase64String(base64Data);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName);
        }

        // Kiểm tra string có phải base64 không
        private bool IsBase64String(string base64)
            {
            return base64.Contains("data:image") || base64.Length > 100; // Simple check
            }

        // Các class request models
        public class CreateCategoryRequest
        {
            public string Name { get; set; }

            public string? Image { get; set; }
            public CategoryStatus Status { get; set; }
            public string? Hashtag { get; set; }
            public string? Description { get; set; }
        }

        public class UpdateCategoryRequest
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public string? Image { get; set; }
            public CategoryStatus Status { get; set; }
            public string? Hashtag { get; set; }
            public string? Description { get; set; }
        }
        public async Task<Categories?> GetCategoryByProductIdAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}Categories/GetByProduct/{productId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Categories>();
            }

            Console.WriteLine($"Lỗi khi lấy danh mục theo ProductId {productId}: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }
        public async Task<(bool success, List<CategoryWithUsageViewModel> data, string message)> GetAllWithUsageAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<CategoryWithUsageViewModel>>($"{_baseUrl}Categories/with-usage");
                return (true, data ?? new List<CategoryWithUsageViewModel>(), "Thành công");
            }
            catch (Exception ex)
            {
                return (false, new List<CategoryWithUsageViewModel>(), ex.Message);
            }
        }


    }
}
