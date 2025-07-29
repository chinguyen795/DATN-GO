using DATN_GO.Models;
namespace DATN_GO.ViewModels.Address
{
    public class AddressCreateViewModel
    {
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public string? Description { get; set; }
        public AddressStatus Status { get; set; }

        // Giữ lại nếu bạn cần dùng ID để lưu DB
        public int CityId { get; set; }
        public int DistrictId { get; set; }
        public int WardId { get; set; }

        // ✨ Thêm các field này để binding dữ liệu từ view → JSON
        public string CityName { get; set; } = string.Empty;
        public string DistrictName { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;

        public List<CityViewModel> Cities { get; set; } = new();
        public List<DistrictViewModel> Districts { get; set; } = new();
        public List<WardViewModel> Wards { get; set; } = new();
    }


}