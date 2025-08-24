using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DATN_API.Services
{
    public class OcrService : IOcrService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public OcrService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _context = context;
        }
        public async Task<string> ExtractFromImageAsync(IFormFile imageFile)
        {
            var client = _httpClientFactory.CreateClient();

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageFile.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);

            content.Add(streamContent, "image", imageFile.FileName);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.fpt.ai/vision/idr/vnm")
            {
                Content = content
            };

            request.Headers.Add("api-key", _configuration["FptAI:ApiKey"]);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Phản hồi FPT: " + result);

            // Parse lại đúng với format mới (data là mảng)
            try
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;
                var dataArr = root.GetProperty("data");
                if (dataArr.ValueKind == JsonValueKind.Array && dataArr.GetArrayLength() > 0)
                {
                    var first = dataArr[0];
                    var citizenId = first.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    var name = first.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var address = first.TryGetProperty("address", out var addrProp) ? addrProp.GetString() : null;
                    var mapped = new
                    {
                        citizenIdentityCard = citizenId,
                        representativeName = name,
                        address = address
                    };
                    return JsonSerializer.Serialize(mapped);
                }
                return JsonSerializer.Serialize(new { citizenIdentityCard = (string?)null, representativeName = (string?)null, address = (string?)null });
            }
            catch
            {
                // Nếu lỗi parse thì trả về chuỗi gốc
                return result;
            }
        }




        public IActionResult SaveInfoFromOcr(OcrSaveInfoRequest request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user == null)
                return new NotFoundObjectResult(new { message = "Không tìm thấy người dùng." });

            user.CitizenIdentityCard = request.CitizenIdentityCard;
            user.UpdateAt = DateTime.Now;

            var store = _context.Stores.FirstOrDefault(s => s.UserId == request.UserId);
            if (store == null)
            {
                store = new Stores
                {
                    UserId = request.UserId,
                    RepresentativeName = request.RepresentativeName,
                    Address = request.Address,
                    Avatar = request.AvatarUrl,
                    CoverPhoto = request.CoverUrl,
                    Name = request.Name ?? "Cửa hàng chưa đặt tên",
                    Bank = request.Bank,
                    BankAccount = request.BankAccount,
                    BankAccountOwner = request.BankAccountOwner,
                    Status = StoreStatus.PendingApproval,
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,

                    // Các trường mới 
                    Province = request.Province,
                    District = request.District,
                    Ward = request.Ward,
                    PickupAddress = request.PickupAddress
                };
                _context.Stores.Add(store);
            }
            else
            {
                store.RepresentativeName = request.RepresentativeName;
                store.Address = request.Address;
                store.Avatar = request.AvatarUrl;
                store.CoverPhoto = request.CoverUrl;
                store.Name = request.Name ?? store.Name;
                store.Bank = request.Bank;
                store.BankAccount = request.BankAccount;
                store.BankAccountOwner = request.BankAccountOwner;
                store.Status = StoreStatus.PendingApproval;
                store.UpdateAt = DateTime.Now;

                // Các trường mới
                store.Province = request.Province;
                store.District = request.District;
                store.Ward = request.Ward;
            }

            _context.SaveChanges();
            return new OkObjectResult(new { message = "Lưu thông tin thành công." });
        }

    }
}