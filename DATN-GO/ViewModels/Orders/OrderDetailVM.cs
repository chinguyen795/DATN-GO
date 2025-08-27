using System.Text.Json.Serialization;

namespace DATN_GO.ViewModels.Orders
{
    public class OrderDetailVM
    {
        [JsonPropertyName("id")]
        public int OrderId { get; set; }

        // API trả CreatedAt nên map về OrderDate cho View dùng
        [JsonPropertyName("createdAt")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = "";

        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("shippingMethodName")]
        public string? ShippingMethodName { get; set; }

        // Tổng đã lưu ở server (nếu = 0 thì client sẽ tự cộng ItemsTotal + DeliveryFee)
        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        // Phí ship: API field shippingFee -> map về DeliveryFee cho View
        [JsonPropertyName("shippingFee")]
        public decimal DeliveryFee { get; set; }

        // Mã vận đơn GHTK
        [JsonPropertyName("labelId")]
        public string? LabelId { get; set; }
        [JsonPropertyName("voucherReduce")]
        public decimal VoucherReduce { get; set; }    // ✅ NEW

        [JsonPropertyName("voucherName")]
        public string? VoucherName { get; set; }


        // Danh sách item trả về từ API
        [JsonPropertyName("orderDetails")]
        public List<OrderDetailItemVM> Items { get; set; } = new();

        // Tạm tính (client-side)
        [JsonIgnore]
        public decimal ItemsTotal => Items?.Sum(i => i.SubTotal) ?? 0m;
    }

    public class OrderDetailItemVM
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = "";

        [JsonPropertyName("productImage")]
        public string? Image { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        // API field unitPrice -> map về Price
        [JsonPropertyName("unitPrice")]
        public decimal Price { get; set; }


        [JsonIgnore]
        public decimal SubTotal => Price * Quantity;
    }
}
