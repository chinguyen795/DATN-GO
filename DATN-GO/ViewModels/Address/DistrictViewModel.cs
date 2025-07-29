
namespace DATN_GO.ViewModels.Address
{
    public class DistrictViewModel
    {
        public int Id { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public List<WardViewModel> Wards { get; set; } = new();
    }

}
