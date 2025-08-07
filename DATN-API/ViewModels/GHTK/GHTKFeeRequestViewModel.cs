namespace DATN_API.ViewModels.GHTK
{
    public class GHTKFeeRequestViewModel
    {
        public string PickProvince { get; set; }
        public string PickDistrict { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Address { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
        public string DeliverOption { get; set; }
    }
    public class GHTKShippingFeeFromDbRequest
    {
        public int AddressId { get; set; }
        public int StoreId { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
    }

}
