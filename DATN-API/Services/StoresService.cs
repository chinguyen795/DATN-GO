using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class StoresService : IStoresService
    {
        private readonly ApplicationDbContext _context;
        public StoresService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Stores>> GetAllAsync()
        {
            return await _context.Stores.ToListAsync();
        }

        public async Task<Stores> GetByIdAsync(int id)
        {
            return await _context.Stores.FirstOrDefaultAsync(s => s.Id == id);
        }
        public async Task<Stores?> GetStoreByUserIdAsync(int userId)
        {
            return await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        }
        public async Task<Stores> CreateAsync(Stores model)
        {
            _context.Stores.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Stores model)
        {
            if (id != model.Id) return false;

            var store = await _context.Stores.FindAsync(id);
            if (store == null) return false;

            store.Name = model.Name;
            store.Status = model.Status;
            store.Address = model.Address;
            store.CoverPhoto = model.CoverPhoto;
            store.Avatar = model.Avatar;
            store.Bank = model.Bank;
            store.BankAccount = model.BankAccount;
            store.Longitude = model.Longitude;
            store.Latitude = model.Latitude;
            store.Slug = model.Slug;
            store.Rating = model.Rating;
            store.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return false;
            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<int> GetTotalStoresAsync()
        {
            return await _context.Stores.CountAsync();
        }

        public async Task<int> GetTotalActiveStoresAsync()
        {
            return await _context.Stores.CountAsync(s => s.Status == StoreStatus.Active);
        }
        public async Task<int> GetStoreCountByMonthYearAsync(int month, int year)
        {
            return await _context.Stores
                .CountAsync(s => s.CreateAt.Month == month && s.CreateAt.Year == year);
        }
        public async Task<Dictionary<int, int>> GetStoreCountByMonthAsync(int year)
        {
            // L?y d? li?u th?t t? DB
            var storeCounts = await _context.Stores
                .Where(s => s.CreateAt.Year == year)
                .GroupBy(s => s.CreateAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Month, x => x.Count);

            // ??m b?o ?? 12 tháng, n?u thi?u thì thêm v?i giá tr? 0
            var fullMonthData = Enumerable.Range(1, 12)
                .ToDictionary(month => month, month => storeCounts.ContainsKey(month) ? storeCounts[month] : 0);

            return fullMonthData;
        }


        public async Task<Stores?> GetByUserIdAsync(int userId)
        {
            return await _context.Stores.FirstOrDefaultAsync(s => s.UserId == userId);
        }

    }
}
