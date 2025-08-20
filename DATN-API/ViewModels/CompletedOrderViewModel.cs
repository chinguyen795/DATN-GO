namespace DATN_API.ViewModels
{
    public class CompletedOrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public List<CompletedOrderProduct> Products { get; set; }
    }

    public class CompletedOrderProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
    }

}