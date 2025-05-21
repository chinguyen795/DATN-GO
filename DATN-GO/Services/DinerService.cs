using DATN_GO.Models;
using DATN_GO.Service;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using DATN_GO.Areas.Seller.Controllers;

namespace DATN_GO.Services
{
    public class DinerService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConfiguration _configuration;
        private readonly GoogleCloudStorageService _gcsService;

        public DinerService(IHttpContextAccessor contextAccessor, IConfiguration configuration, GoogleCloudStorageService gcsService)
        {
            _contextAccessor = contextAccessor;
            _configuration = configuration;
            _gcsService = gcsService;

            _client = new HttpClient
            {
                BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"])
            };
        }

        public async Task<Diners> Get()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "Diners/ByUser");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _contextAccessor.GetToken());
            var response = await _client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new Exception("Đã có lỗi xảy ra trong quá trình xử lý");

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Diners>(content);
        }

        public async Task<Diners> Create(DinnerModel model)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "Diners");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _contextAccessor.GetToken());
            request.Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception("Đã có lỗi xảy ra trong quá trình xử lý");

            return JsonConvert.DeserializeObject<Diners>(content);
        }

        public async Task<Diners> Update(int id, DinnerModel model)
        {
            model.Id = id;
            var request = new HttpRequestMessage(HttpMethod.Put, $"Diners/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _contextAccessor.GetToken());
            request.Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Đã có lỗi xảy ra trong quá trình xử lý");

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Diners>(content);
        }

        public async Task UpdateImage(ChangeImageModel model)
        {
            var fileName = $"{Guid.NewGuid()}.jpg";

            // 💡 Đây là nơi cần đảm bảo folder đúng
            var folder = model.IsAvatar ? "sellers/avatar/" : "sellers/cover/";

            var formFile = ConvertBase64ToFormFile(model.Data, fileName);

            var imageUrl = await _gcsService.UploadFileAsync(formFile, folder);
            if (string.IsNullOrEmpty(imageUrl))
                throw new Exception("Upload thất bại");

            var apiModel = new ChangeImageModel
            {
                IsAvatar = model.IsAvatar,
                Data = imageUrl
            };

            var request = new HttpRequestMessage(HttpMethod.Put, "Diners/ChangeImage");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _contextAccessor.GetToken());
            request.Content = new StringContent(JsonConvert.SerializeObject(apiModel), Encoding.UTF8, "application/json");

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Không thể cập nhật ảnh");
        }



        private IFormFile ConvertBase64ToFormFile(string base64String, string fileName)
        {
            var base64Parts = base64String.Split(',');
            var base64Data = base64Parts.Length > 1 ? base64Parts[1] : base64Parts[0];
            var bytes = Convert.FromBase64String(base64Data);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName);
        }


  



    }
}
