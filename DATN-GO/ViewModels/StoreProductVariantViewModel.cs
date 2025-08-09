using DATN_GO.Models;

namespace DATN_GO.ViewModels
{
    public class StoreProductVariantViewModel
    {
        // Stores
        public int StoreId { get; set; }
        public int StoreUserId { get; set; }
        public string StoreName { get; set; }
        public string? StoreRepresentativeName { get; set; }
        public string? StoreAddress { get; set; }
        public float StoreLongitude { get; set; }
        public float StoreLatitude { get; set; }
        public string? StoreAvatar { get; set; }
        public StoreStatus StoreStatus { get; set; }
        public string? StoreSlug { get; set; }
        public string? StoreCoverPhoto { get; set; }
        public string? StoreBankAccount { get; set; }
        public string? StoreBank { get; set; }
        public string? StoreBankAccountOwner { get; set; }
        public float StoreRating { get; set; }
        public DateTime StoreCreateAt { get; set; }
        public DateTime StoreUpdateAt { get; set; }

        // Products
        public int ProductId { get; set; }
        public int ProductCategoryId { get; set; }
        public int ProductStoreId { get; set; }
        public string ProductName { get; set; }
        public string? ProductBrand { get; set; }
        public int? ProductWeight { get; set; }
        public string? ProductSlug { get; set; }
        public string? ProductDescription { get; set; }
        public string? ProductMainImage { get; set; }
        public string? ProductStatus { get; set; }
        public int ProductQuantity { get; set; }
        public int ProductViews { get; set; }
        public float? ProductRating { get; set; }
        public DateTime ProductCreateAt { get; set; }
        public DateTime ProductUpdateAt { get; set; }
        public decimal? ProductCostPrice { get; set; }
        public float? ProductHeight { get; set; }
        public float? ProductWidth { get; set; }
        public float? ProductLength { get; set; }
        public string? ProductPlaceOfOrigin { get; set; }
        public string? ProductHashtag { get; set; }

        // ProductVariants
        public int ProductVariantId { get; set; }
        public int ProductVariantProductId { get; set; }
        public decimal ProductVariantPrice { get; set; }
        public int ProductVariantWeight { get; set; }
        public DateTime ProductVariantCreatedAt { get; set; }
        public DateTime ProductVariantUpdatedAt { get; set; }
        public decimal ProductVariantCostPrice { get; set; }
        public string? ProductVariantImage { get; set; }
        public int ProductVariantQuantity { get; set; }
        public string? ProductVariantPackageSize { get; set; }

        // VariantValues
        public int VariantValueId { get; set; }
        public int VariantValueVariantId { get; set; }
        public string VariantValueValueName { get; set; }
        public string VariantValueType { get; set; }
        public string? VariantValueImage { get; set; }
        public string? VariantValueColorHex { get; set; }

        // Variants
        public int VariantId { get; set; }
        public int VariantProductId { get; set; }
        public string VariantVariantName { get; set; }
        public string VariantType { get; set; }

        // VariantComposition
        public int VariantCompositionId { get; set; }
        public int? VariantCompositionProductVariantId { get; set; }
        public int? VariantCompositionVariantValueId { get; set; }
        public int? VariantCompositionVariantId { get; set; }
    }
    public class VariantDisplayGroup
    {
        public string VariantName { get; set; }
        public List<string> Values { get; set; }
    }
    public class VariantWithValuesViewModel
    {
        public string VariantName { get; set; }
        public string VariantType { get; set; }
        public List<VariantValueItem> Values { get; set; }
    }

    public class VariantValueItem
    {
        public int Id { get; set; }
        public string ValueName { get; set; }
        public string? ColorHex { get; set; }
    }

    public class VariantCombinationViewModel
    {
        public int ProductVariantId { get; set; }
        public decimal Price { get; set; }
        public string? Image { get; set; }
        public int Quantity { get; set; }
        public List<int> VariantValueIds { get; set; } = new List<int>();
    }

    public class MinMaxPriceResponse
    {
        public bool IsVariant { get; set; }
        public decimal? Price { get; set; }           
        public decimal? MinPrice { get; set; }      
        public decimal? MaxPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
    }

}
