
namespace DATN_API.ViewModels.Orders
{
    public class OrderDetailItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? Image { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal => Price * Quantity;
    }
}
