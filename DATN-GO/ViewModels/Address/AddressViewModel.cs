using DATN_GO.Models;
namespace DATN_GO.ViewModels.Address
{
    public class AddressViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public string? Description { get; set; }
        public AddressStatus Status { get; set; }

        public string? CityName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
    }

}