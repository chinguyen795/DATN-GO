using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATN_API.Models
{
    public class CartItemVariants
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Cart")]
        public int CartId { get; set; }

        [ForeignKey("VariantValue")]
        public int VariantValueId { get; set; }

        public virtual Carts Cart { get; set; }
        public virtual VariantValues VariantValue { get; set; }
    }
}
