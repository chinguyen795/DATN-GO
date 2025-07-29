namespace DATN_GO.ViewModels.Address
{
    public class CityViewModel
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public List<DistrictViewModel> Districts { get; set; } = new();
    }


}