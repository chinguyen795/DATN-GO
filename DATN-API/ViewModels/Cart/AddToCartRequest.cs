namespace DATN_API.ViewModels.Cart
{
    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public List<int> VariantValueIds { get; set; }
        public int Quantity { get; set; }
    }

}
