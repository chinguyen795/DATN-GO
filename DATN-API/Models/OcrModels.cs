using System.Text.Json.Serialization;

namespace DATN_API.Models
{
    public class OcrRequest
    {
        public IFormFile ImageFile { get; set; }
    }

    public class OcrSaveInfoRequest
    {
        public int UserId { get; set; }
        public string? CitizenIdentityCard { get; set; }
        public string? RepresentativeName { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CoverUrl { get; set; }
        public string? Name { get; set; }

        public string? BankAccount { get; set; }
        public string? Bank { get; set; }
        public string? BankAccountOwner { get; set; }

        // Thêm các trường Province, District, Ward
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? PickupAddress { get; set; }
        public string? PhoneNumber { get; set; }
    }

}