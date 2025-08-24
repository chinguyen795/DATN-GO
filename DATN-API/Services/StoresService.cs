using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels;
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
            if (model == null || id != model.Id) return false;

            var store = await _context.Stores.FindAsync(id);
            if (store == null) return false;

            // Basic
            store.Name = model.Name;
            store.Status = model.Status;

            // Địa lý & địa chỉ (mới)
            store.Ward = model.Ward;
            store.District = model.District;
            store.Province = model.Province;
            store.PickupAddress = model.PickupAddress;
            // store.Address = model.Address; // ĐÃ BỎ

            // Media & banking
            store.CoverPhoto = model.CoverPhoto;
            store.Avatar = model.Avatar;
            store.Bank = model.Bank;
            store.BankAccount = model.BankAccount;
            // Nếu dùng luôn thì mở:
            // store.BankAccountOwner = model.BankAccountOwner;

            // Location & misc
            store.Longitude = model.Longitude;
            store.Latitude = model.Latitude;
            store.Slug = model.Slug;
            store.Rating = model.Rating;

            // Audit
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

        public async Task<IEnumerable<Stores>> GetByStatusAsync(string status)
        {
            if (!Enum.TryParse<StoreStatus>(status, true, out var parsedStatus))
                return Enumerable.Empty<Stores>();

            return await _context.Stores
                .Where(s => s.Status == parsedStatus)
                .Include(s => s.User)
                .ToListAsync();
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return false;

            if (!Enum.TryParse<StoreStatus>(status, true, out var parsedStatus))
                return false;

            store.Status = parsedStatus;
            store.UpdateAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<AdminStorelViewModels?> GetAdminDetailAsync(int id)
        {
            var store = await _context.Stores
                .Include(s => s.User) // load chủ cửa hàng
                .Include(s => s.Products)
                    .ThenInclude(p => p.ProductVariants) // load variants (có Price ở đây)
                .Include(s => s.Products)
                    .ThenInclude(p => p.Prices)          // load prices (sp không có variant)
                .Include(s => s.Products)
                    .ThenInclude(p => p.Category)        // load category
                .Include(s => s.Products)
                    .ThenInclude(p => p.OrderDetails)
                        .ThenInclude(od => od.Order)
                            .ThenInclude(o => o.User)    // khách hàng
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return null;

            return new AdminStorelViewModels
            {
                Id = store.Id,
                Name = store.Name,
                OwnerName = store.RepresentativeName,
                OwnerEmail = store.User?.Email,
                Avatar = store.Avatar,
                CoverPhoto = store.CoverPhoto,
                Address = store.Address,
                Status = store.Status.ToString(),
                CreateAt = store.CreateAt,
                UpdateAt = store.UpdateAt,
                BankAccount = store.BankAccount,
                AccountHolder = store.BankAccountOwner,
                BankName = store.Bank,

                // Danh sách sản phẩm
                Products = store.Products.Select(p =>
                {
                    // Nếu có variant → lấy min/max từ variant
                    if (p.ProductVariants != null && p.ProductVariants.Any(v => v.Price > 0))
                    {
                        var minPrice = p.ProductVariants.Min(v => v.Price);
                        var maxPrice = p.ProductVariants.Max(v => v.Price);
                        var totalStock = p.ProductVariants.Sum(v => v.Quantity);

                        return new StoreProductViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Price = minPrice,   // gán giá mặc định là giá thấp nhất
                            MinPrice = minPrice,
                            MaxPrice = maxPrice,
                            Stock = totalStock,
                            Status = p.Status.ToString(),
                            Image = p.MainImage,
                            Category = p.Category?.Name
                        };
                    }

                    // Nếu không có variant → lấy giá từ Prices
                    decimal productPrice = 0;
                    if (p.Prices != null && p.Prices.Any())
                        productPrice = p.Prices.Min(pr => pr.Price);

                    return new StoreProductViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = productPrice,
                        MinPrice = null,
                        MaxPrice = null,
                        Stock = p.Quantity,
                        Status = p.Status.ToString(),
                        Image = p.MainImage,
                        Category = p.Category?.Name
                    };
                }).ToList(),

                // Danh sách đơn hàng (qua OrderDetails -> Orders)
                Orders = store.Products
                    .SelectMany(p => p.OrderDetails ?? new List<OrderDetails>())
                    .Where(od => od.Order != null)
                    .GroupBy(od => od.Order.Id) // tránh trùng đơn
                    .Select(g => g.First().Order!)
                    .Select(o => new StoreOrderViewModel
                    {
                        Id = o.Id,
                        CustomerName = o.User?.FullName,
                        Status = o.Status.ToString(),
                        CreateAt = o.OrderDate,
                        TotalAmount = o.TotalPrice
                    }).ToList()
            };
        }

        public async Task<IEnumerable<AdminStorelViewModels>> GetAllAdminStoresAsync()
        {
            return await _context.Stores
                .Include(s => s.User)
                .Select(s => new AdminStorelViewModels
                {
                    Id = s.Id,
                    Name = s.Name,
                    OwnerName = s.User.FullName,
                    OwnerEmail = s.User.Email,
                    Avatar = s.Avatar,
                    CoverPhoto = s.CoverPhoto,
                    Address = s.Address,
                    Status = s.Status.ToString(),
                    CreateAt = s.CreateAt,
                    UpdateAt = s.UpdateAt,
                    BankAccount = s.BankAccount,
                    AccountHolder = s.BankAccountOwner,
                    BankName = s.Bank
                }).ToListAsync();
        }
    }
}
