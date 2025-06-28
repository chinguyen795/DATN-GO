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
    }
}
