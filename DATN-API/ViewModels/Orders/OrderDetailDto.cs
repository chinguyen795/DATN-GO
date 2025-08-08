using DATN_API.Models;

namespace DATN_API.ViewModels.Orders
{
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public OrderStatus Status { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal ItemsTotal { get; set; }
        public decimal TotalPrice { get; set; }

        public string? ShippingMethodName { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";

        public List<OrderDetailItemDto> Items { get; set; } = new();
    }
}
 