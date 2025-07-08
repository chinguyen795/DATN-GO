using Microsoft.AspNetCore.Http;

namespace DATN_GO.Models
{
    public class OcrRequest
    {
        public IFormFile ImageFile { get; set; }
    }

    public class OcrResultModel
    {
        public string? CitizenIdentityCard { get; set; }
        public string? RepresentativeName { get; set; }
        public string? Address { get; set; }
    }
}
