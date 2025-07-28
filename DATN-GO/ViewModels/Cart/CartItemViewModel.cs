namespace DATN_GO.ViewModels.Cart
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
    }

}
