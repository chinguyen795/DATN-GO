namespace DATN_API.ViewModels.Orders
{
    public class OrderViewModel
    {
        public int Id { get; set; }

        // MVC map -> createdAt
        public DateTime CreatedAt { get; set; }

        public string PaymentMethod { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string Status { get; set; } = "";

        public string? ShippingMethodName { get; set; }

        // QUAN TRỌNG: trả đúng phí ship đã lưu trên đơn
        public decimal ShippingFee { get; set; }

        // Tổng tiền đã lưu
        public decimal TotalPrice { get; set; }

        // QUAN TRỌNG: mã vận đơn GHTK
        public string? LabelId { get; set; }

        public List<OrderDetailViewModel> OrderDetails { get; set; } = new();
    }

    public class OrderDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}