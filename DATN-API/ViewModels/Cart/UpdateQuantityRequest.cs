namespace DATN_API.ViewModels.Cart
{
    public class UpdateQuantityRequest
    {
        public int CartId { get; set; }
        public int NewQuantity { get; set; }
    }
}
