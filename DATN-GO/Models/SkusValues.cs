using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DATN_GO.Models
{
    public class SkusValues
    {
        [ForeignKey("OptionValue")]
        public int ValueId { get; set; }

        [ForeignKey("ProductSku")]
        public int SkuId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [ForeignKey("Option")]
        public int OptionId { get; set; }
        [JsonIgnore]
        public virtual OptionValues? OptionValue { get; set; }
        [JsonIgnore]
        public virtual ProductSkus? ProductSku { get; set; }
        [JsonIgnore]
        public virtual Products? Product { get; set; }
        [JsonIgnore]
        public virtual Options? Option { get; set; }
    }
}
