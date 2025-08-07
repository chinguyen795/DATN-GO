using DATN_GO.ViewModels.Address;

namespace DATN_GO.ViewModels.Cart
{
    public class CartSummaryViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public List<AddressViewModel> Addresses { get; set; }
        public List<UserVoucherViewModel> Vouchers { get; set; }
    }

    public class UserVoucherViewModel
    {
        public int Id { get; set; }
        public int VoucherId { get; set; }
        public decimal Reduce { get; set; }
        public decimal MinOrder { get; set; }
        public DateTime EndDate { get; set; }
        public string StoreName { get; set; }
    }
}
