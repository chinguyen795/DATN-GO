namespace DATN_GO.Models
{
    public class Statistics
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int CompletedOrders { get; set; }

        public double? TotalOrdersPercentChange { get; set; }
        public double? PendingOrdersPercentChange { get; set; }
        public double? ShippingOrdersPercentChange { get; set; }
        public double? CompletedOrdersPercentChange { get; set; }
    }
}