using DATN_GO.ViewModels.GHTK;
using Newtonsoft.Json;

namespace DATN_GO.ViewModels
{

    public class OrderViewModel
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? StoreName { get; set; }
        public string? VoucherName { get; set; }
        public decimal? VoucherReduce { get; set; }
        public string? LabelId { get; set; }
        public GHTKOrderStatusViewModel GHTKStatus { get; set; }
        public string ShippingMethodName { get; set; } = string.Empty;
        public decimal ShippingFee { get; set; }

        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }

        public string Status { get; set; } = string.Empty;
        [JsonProperty("orderDetails")]
        public List<OrderDetailViewModel> OrderDetails { get; set; } = new();

        public int TotalQuantity => OrderDetails.Sum(d => d.Quantity);
        public decimal GrandTotal => TotalPrice  - (VoucherReduce ?? 0);
    }

    public class OrderDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public string? ProductImage { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
    public class UserSummaryViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Phone { get; set; }
    }

    public class ProductSummaryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MainImage { get; set; }
        public decimal? CostPrice { get; set; }
    }
}