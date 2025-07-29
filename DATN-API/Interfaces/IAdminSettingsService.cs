using DATN_API.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Interfaces
{
    public interface IAdminSettingsService
    {
        // Get all AdminSettings
        Task<IEnumerable<AdminSettings>> GetAllAsync();

        // Get AdminSettings by ID
        Task<AdminSettings> GetByIdAsync(int id);

        // Create a new AdminSetting
        Task<AdminSettings> CreateAsync(AdminSettings model);

        // Update an existing AdminSetting
        Task<bool> UpdateAsync(int id, AdminSettings model);

        // Delete an AdminSetting by ID
        Task<bool> DeleteAsync(int id);
    }
}