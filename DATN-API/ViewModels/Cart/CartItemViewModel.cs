namespace DATN_API.ViewModels.Cart
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Total => Quantity * Price;
        public int MaxQuantity { get; set; }
        public List<string> Variants { get; set; }
        public bool IsSelected { get; set; }
        public int TotalWeight { get; set; }  
        public int TotalValue { get; set; }
        public int StoreId { get; set; }
    }

    public class StoreCartGroup
    {
        public int StoreId { get; set; }
        public List<StoreCartItem> Products { get; set; } = new();
        public int TotalWeight => Products.Sum(p => p.TotalWeight);
        public int TotalValue => Products.Sum(p => p.Price * p.Quantity);
    }

    public class StoreCartItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int TotalWeight { get; set; }
    }

    public class ShippingGroupViewModel
    {
        public int StoreId { get; set; }
        public int TotalWeight { get; set; }
        public int TotalValue { get; set; }
        public List<int> ProductIds { get; set; }
        public int ShippingFee { get; set; }
    }
    public class ShippingGroupRequest
    {
        public int UserId { get; set; }
        public int AddressId { get; set; }
    }

}
