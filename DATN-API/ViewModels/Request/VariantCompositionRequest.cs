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

}
