namespace DATN_API.ViewModels.Cart
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
        public int MaxQuantity { get; set; }
        public List<string> Variants { get; set; }
        public bool IsSelected { get; set; }
        public decimal TotalWeight { get; set; }  // Thay đổi từ int sang decimal
        public decimal TotalValue { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string StoreAvatar { get; set; }
    }

    public class StoreCartGroup
    {
        public int StoreId { get; set; }
        public List<StoreCartItem> Products { get; set; } = new();
        public decimal TotalWeight => Products.Sum(p => p.TotalWeight); // decimal
        public decimal TotalValue => Products.Sum(p => p.Price * p.Quantity);
    }

    public class StoreCartItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalWeight { get; set; } // Thay đổi từ int sang decimal
    }

    public class ShippingGroupViewModel
    {
        public int StoreId { get; set; }
        public decimal TotalWeight { get; set; } // Thay đổi từ int sang decimal
        public decimal TotalValue { get; set; }
        public List<int> ProductIds { get; set; }
        public int ShippingFee { get; set; }
    }

    public class ShippingGroupRequest
    {
        public int UserId { get; set; }
        public int AddressId { get; set; }
    }
}