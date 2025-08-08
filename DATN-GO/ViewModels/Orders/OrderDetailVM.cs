using System.Text.Json.Serialization;

namespace DATN_GO.ViewModels.Orders
{
    public class OrderDetailVM
    {
        [JsonPropertyName("id")]
        public int OrderId { get; set; }

        [JsonPropertyName("createdAt")] // <- đổi từ orderDate
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod { get; set; } = "";

        [JsonPropertyName("paymentStatus")]
        public string PaymentStatus { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("shippingMethodName")]
        public string? ShippingMethodName { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("shippingFee")] // <- đổi từ deliveryFee
        public decimal DeliveryFee { get; set; }

        [JsonPropertyName("orderDetails")]
        public List<OrderDetailItemVM> Items { get; set; } = new();

        // Tự tính tạm tính (ItemsTotal) ở client
        [JsonIgnore]
        public decimal ItemsTotal => Items?.Sum(i => i.SubTotal) ?? 0;
    }

    public class OrderDetailItemVM
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = "";

        [JsonPropertyName("productImage")] // <- đổi từ image
        public string? Image { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")] // <- đổi từ price
        public decimal Price { get; set; }

        [JsonIgnore]
        public decimal SubTotal => Price * Quantity;
    }
}
