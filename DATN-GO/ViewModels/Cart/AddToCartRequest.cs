namespace DATN_GO.ViewModels.Cart
{
    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public List<int> VariantValueIds { get; set; }
        public int Quantity { get; set; }
    }

}
