namespace DATN_API.ViewModels.Request
{
    public class VariantCompositionRequest
    {
        public int ProductId { get; set; }
        public int ProductVariantId { get; set; }

        public List<VariantPairRequest> VariantPairs { get; set; } = new();
    }

    public class VariantPairRequest
    {
        public int VariantId { get; set; }
        public int VariantValueId { get; set; }
    }

    public class VariantCombinationViewModel
    {
        public int ProductVariantId { get; set; }
        public decimal Price { get; set; }
        public string? Image { get; set; }
        public int Quantity { get; set; }
        public List<int> VariantValueIds { get; set; } = new List<int>();
    }


}
