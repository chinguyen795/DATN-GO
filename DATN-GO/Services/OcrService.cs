using System.Net.Http.Headers;
using System.Text.Json;
using DATN_GO.Models;
using Microsoft.Extensions.Configuration;

namespace DATN_GO.Services
{
    public class OcrService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public OcrService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<OcrResultModel?> ExtractFromImageAsync(IFormFile imageFile)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageFile.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
            content.Add(streamContent, "ImageFile", imageFile.FileName);

            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api/";
            var apiUrl = baseUrl.TrimEnd('/') + "/ocr/cccd";
            var response = await _httpClient.PostAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var result = JsonSerializer.Deserialize<OcrResultModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result;
            }
            catch
            {
                try
                {
                    var inner = JsonSerializer.Deserialize<string>(json);
                    if (!string.IsNullOrEmpty(inner))
                    {
                        var result = JsonSerializer.Deserialize<OcrResultModel>(inner, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        return result;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public async Task<bool> SaveInfoFromOcrAsync(object ocrSaveInfoRequest)
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7096/api/";
            var apiUrl = baseUrl.TrimEnd('/') + "/ocr/save-info";
            var jsonContent = new StringContent(JsonSerializer.Serialize(ocrSaveInfoRequest), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, jsonContent);
            return response.IsSuccessStatusCode;
        }
    }
}
