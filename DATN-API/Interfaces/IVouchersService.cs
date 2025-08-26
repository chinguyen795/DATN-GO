using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IVouchersService
    {
        Task<IEnumerable<Vouchers>> GetAllVouchersAsync();
        Task<Vouchers?> GetVoucherByIdAsync(int id);
        Task<Vouchers> CreateVoucherAsync(Vouchers voucher);
        Task<Vouchers?> UpdateVoucherAsync(int id, Vouchers voucher);
        Task<bool> DeleteVoucherAsync(int id);
      
            Task<IEnumerable<Vouchers>> GetVouchersByStoreOrAdminAsync(int? storeId);

        (decimal discountOnSubtotal, decimal discountOnShipping, string reason) ApplyVoucher(
             Vouchers v,
             decimal orderSubtotal,
             IEnumerable<int> productIdsInCart,
             int? categoryIdInCart);

        Task<(bool ok, string reason)> RedeemVoucherAsync(int voucherId);
            string? ValidateForCreateOrUpdate(Vouchers v, bool isCreate);
        }

    
}
