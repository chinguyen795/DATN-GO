using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;

namespace DATN_API.Services
{
    public class DeliveryTrackingsService : IDeliveryTrackingsService
    {
        private readonly ApplicationDbContext _context;
        public DeliveryTrackingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DeliveryTrackings>> GetAllAsync()
        {
            return await _context.DeliveryTrackings.Include(dt => dt.Order).ToListAsync();
        }

        public async Task<DeliveryTrackings?> GetByIdAsync(int id)
        {
            return await _context.DeliveryTrackings.Include(dt => dt.Order).FirstOrDefaultAsync(dt => dt.Id == id);
        }

        public async Task<DeliveryTrackings?> GetByOrderIdAsync(int orderId)
        {
            return await _context.DeliveryTrackings.Include(dt => dt.Order).FirstOrDefaultAsync(dt => dt.OrderId == orderId);
        }

        public async Task<DeliveryTrackings> CreateAsync(DeliveryTrackings model)
        {
            model.CreateAt = DateTime.UtcNow;
            _context.DeliveryTrackings.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, DeliveryTrackings model)
        {
            var tracking = await _context.DeliveryTrackings.FindAsync(id);
            if (tracking == null) return false;
            tracking.OrderId = model.OrderId;
            tracking.AhamoveOrderId = model.AhamoveOrderId;
            tracking.ServiceId = model.ServiceId;
            tracking.TrackingUrl = model.TrackingUrl;
            tracking.DriverName = model.DriverName;
            tracking.DriverPhone = model.DriverPhone;
            tracking.EstimatedTime = model.EstimatedTime;
            tracking.Status = model.Status;
            tracking.CreateAt = model.CreateAt;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tracking = await _context.DeliveryTrackings.FindAsync(id);
            if (tracking == null) return false;
            _context.DeliveryTrackings.Remove(tracking);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
