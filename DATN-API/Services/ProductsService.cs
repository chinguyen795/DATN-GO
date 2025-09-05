using DATN_API.Data;
using DATN_API.Models;
using DATN_API.Services.Interfaces;
using DATN_API.ViewModels;
using DATN_GO.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ProductsService : IProductsService
    {
        private readonly ApplicationDbContext _context;
        public ProductsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Products>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Products> GetByIdAsync(int id)
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Products> CreateAsync(Products model)
        {
            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<bool> UpdateAsync(int id, Products model)
        {
            if (id != model.Id) return false;

            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            // Cập nhật đầy đủ các thuộc tính theo đúng Models
            product.CategoryId = model.CategoryId;
            product.StoreId = model.StoreId;
            product.Name = model.Name;
            product.Brand = model.Brand;
            product.Weight = model.Weight;
            product.Slug = model.Slug;
            product.Description = model.Description;
            product.MainImage = model.MainImage;
            product.Status = model.Status;
            product.Quantity = model.Quantity;
            product.Views = model.Views;
            product.Rating = model.Rating;
            product.CostPrice = model.CostPrice;
            product.Length = model.Length;
            product.Width = model.Width;
            product.Height = model.Height;
            product.PlaceOfOrigin = model.PlaceOfOrigin;
            product.Hashtag = model.Hashtag;

            product.UpdateAt = DateTime.Now; // Cập nhật thời gian sửa đổi

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<int> GetTotalProductsAsync()
        {
            return await _context.Products.CountAsync();
        }
        public async Task<Dictionary<int, int>> GetProductCountByMonthAsync(int year)
        {
            var rawData = await _context.Products
                .Where(p => p.CreateAt.Year == year)
                .GroupBy(p => p.CreateAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Month, x => x.Count);

            // Đảm bảo đủ 12 tháng
            var result = Enumerable.Range(1, 12)
                .ToDictionary(m => m, m => rawData.ContainsKey(m) ? rawData[m] : 0);

            return result;
        }

        public async Task<List<Products>> GetProductsByStoreAsync(int storeId)
        {
            return await _context.Products
                .Include(p => p.Store)
                .Include(p => p.Category)
                .Where(p => p.StoreId == storeId && p.Status == ProductStatus.Approved)
                .ToListAsync();
        }
        public async Task<List<StoreProductVariantViewModel>> GetAllStoreProductVariantsAsync()
        {
            var result = await (
                from product in _context.Products
                join variant in _context.ProductVariants on product.Id equals variant.ProductId
                join store in _context.Stores on product.StoreId equals store.Id
                join comp in _context.VariantCompositions on variant.Id equals comp.ProductVariantId into compGroup
                from comp in compGroup.DefaultIfEmpty()
                join value in _context.VariantValues on comp.VariantValueId equals value.Id into valueGroup
                from value in valueGroup.DefaultIfEmpty()
                join variantInfo in _context.Variants on comp.VariantId equals variantInfo.Id into variantGroup
                from variantInfo in variantGroup.DefaultIfEmpty()
                select new StoreProductVariantViewModel
                {
                    // Product
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductMainImage = product.MainImage,
                    ProductCategoryId = product.CategoryId,
                    ProductDescription = product.Description,
                    ProductStatus = product.Status.ToString(),
                    ProductSlug = product.Slug,

                    // Store
                    StoreId = store.Id,
                    StoreName = store.Name,
                    StoreAvatar = store.Avatar,
                    StoreUserId = store.UserId,
                    StoreStatus = store.Status,
                    StoreRating = store.Rating,

                    // Variant
                    ProductVariantId = variant.Id,
                    ProductVariantPrice = variant.Price,
                    ProductVariantImage = variant.Image,
                    ProductVariantQuantity = variant.Quantity,
                    ProductVariantHeight = variant.Height,
                    ProductVariantWidth = variant.Width,
                    ProductVariantLength = variant.Length,
                    ProductVariantCreatedAt = variant.CreatedAt,
                    ProductVariantUpdatedAt = variant.UpdatedAt,
                    ProductVariantCostPrice = variant.CostPrice,

                    // Variant Values
                    VariantValueId = value != null ? value.Id : 0,
                    VariantValueValueName = value != null ? value.ValueName : "",
                    VariantValueType = value != null ? value.Type : "",
                    // Variant Info
                    VariantId = variantInfo != null ? variantInfo.Id : 0,
                    VariantVariantName = variantInfo != null ? variantInfo.VariantName : "",
                    VariantType = variantInfo != null ? variantInfo.Type : "",

                    VariantCompositionId = comp != null ? comp.Id : 0,
                    VariantCompositionProductVariantId = comp != null ? comp.ProductVariantId : 0,
                    VariantCompositionVariantValueId = comp != null ? comp.VariantValueId : 0,
                    VariantCompositionVariantId = comp != null ? comp.VariantId : 0
                }
            ).ToListAsync();

            return result;
        }
        public async Task<List<int>> GetProductIdsByStoreIdAsync(int storeId)
        {
            return await _context.Products
                .Where(p => p.StoreId == storeId)
                .Select(p => p.Id)
                .ToListAsync();
        }
        public async Task<List<Products>> GetProductsByStoreIdAsync(int storeId)
        {
            return await _context.Products
                .Where(p => p.StoreId == storeId)
                .ToListAsync();
        }
        public async Task<(bool Success, int? ProductId, string? ErrorMessage)> CreateFullProductAsync(ProductFullCreateViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = model.Product;
                product.CreateAt = DateTime.UtcNow;
                product.UpdateAt = DateTime.UtcNow;
                product.Slug = product.Name.ToLower().Replace(" ", "-") + "-" + DateTime.UtcNow.Ticks;
                product.Status = ProductStatus.Pending;
                product.Rating = 0;
                product.Views = 0;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Nếu không có biến thể
                if (model.Variants == null || model.Combinations == null || !model.Combinations.Any())
                {
                    var price = new Prices
                    {
                        ProductId = product.Id,
                        Price = model.Price ?? 0,
                        VariantCompositionId = null
                    };
                    _context.Prices.Add(price);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var variantDict = new Dictionary<string, (int variantId, int variantValueId)>(StringComparer.OrdinalIgnoreCase);

                    // Tạo Variant và VariantValue
                    foreach (var variant in model.Variants.Take(2))
                    {
                        var variantEntity = new Variants
                        {
                            ProductId = product.Id,
                            VariantName = variant.Name,
                            Type = "Text"
                        };
                        _context.Variants.Add(variantEntity);
                        await _context.SaveChangesAsync();

                        foreach (var val in variant.Values)
                        {
                            var vv = new VariantValues
                            {
                                VariantId = variantEntity.Id,
                                ValueName = val,
                                Type = "Text"
                            };
                            _context.VariantValues.Add(vv);
                            await _context.SaveChangesAsync();

                            variantDict[val.Trim()] = (variantEntity.Id, vv.Id);
                        }
                    }

                    // Tạo ProductVariant + VariantComposition
                    List<ProductVariants> createdVariants = new();

                    foreach (var combo in model.Combinations)
                    {
                        var pv = new ProductVariants
                        {
                            ProductId = product.Id,
                            Price = combo.Price,
                            CostPrice = combo.CostPrice,
                            Quantity = combo.Quantity,
                            Weight = combo.Weight,
                            Width = combo.Width,
                            Height = combo.Height,
                            Length = combo.Length,
                            Image = combo.ImageUrl,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.ProductVariants.Add(pv);
                        await _context.SaveChangesAsync();

                        createdVariants.Add(pv);

                        var pairs = combo.Values
                            .Select(val => val.Trim())
                            .Where(val => variantDict.ContainsKey(val))
                            .Select(val => variantDict[val])
                            .ToList();

                        foreach (var (variantId, variantValueId) in pairs)
                        {
                            _context.VariantCompositions.Add(new VariantComposition
                            {
                                ProductId = product.Id,
                                ProductVariantId = pv.Id,
                                VariantId = variantId,
                                VariantValueId = variantValueId
                            });
                        }

                        await _context.SaveChangesAsync();
                    }

                    // Sau khi tạo hết variant → chọn giá nhỏ nhất
                    var minPrice = createdVariants.Any() ? createdVariants.Min(x => x.Price) : (model.Price ?? 0);

                    var price = new Prices
                    {
                        ProductId = product.Id,
                        Price = minPrice,
                        VariantCompositionId = null
                    };

                    _context.Prices.Add(price);
                    await _context.SaveChangesAsync();

                }

                await transaction.CommitAsync();
                return (true, product.Id, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, null, ex.Message);
            }
        }
        public async Task<bool> DeleteProductAndRelatedAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.VariantCompositions)
                .Include(p => p.Variants)
                .ThenInclude(v => v.VariantValues)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return false;

            // Xoá tất cả VariantCompositions
            foreach (var variant in product.ProductVariants)
            {
                _context.VariantCompositions.RemoveRange(variant.VariantCompositions);
            }

            // Xoá tất cả ProductVariants
            _context.ProductVariants.RemoveRange(product.ProductVariants);

            // Xoá tất cả VariantValues của từng Variant
            foreach (var variant in product.Variants)
            {
                _context.VariantValues.RemoveRange(variant.VariantValues);
            }

            // Xoá tất cả Variants
            _context.Variants.RemoveRange(product.Variants);

            // Xoá sản phẩm
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<ProductAdminViewModel>> GetByStatusAsync(string status)
        {
            // Ensure that status is parsed as integer (0 or 1)
            if (!int.TryParse(status, out var statusValue))
                return Enumerable.Empty<ProductAdminViewModel>();

            var products = await _context.Products
                .Where(p => (int)p.Status == statusValue)  // Cast ProductStatus to int
                .Include(p => p.Category)
                .Include(p => p.Store)
                .Select(p => new ProductAdminViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    MainImage = p.MainImage,
                    Description = p.Description,
                    Brand = p.Brand,
                    Weight = p.Weight,
                    Slug = p.Slug,
                    Status = p.Status.ToString(),
                    Quantity = p.Quantity,
                    Views = p.Views,
                    Rating = p.Rating,
                    CreateAt = p.CreateAt,
                    UpdateAt = p.UpdateAt,
                    CostPrice = p.CostPrice,
                    PlaceOfOrigin = p.PlaceOfOrigin,
                    Hashtag = p.Hashtag,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    StoreName = p.Store != null ? p.Store.Name : null
                })
                .ToListAsync();

            return products;
        }


        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            // Parse status string ("0" or "1") to ProductStatus enum
            if (!Enum.TryParse<ProductStatus>(status, out var parsedStatus))
                return false;

            product.Status = parsedStatus;  // Now assign the parsed status to the Product's Status
            product.UpdateAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<int> GetProductCountByStoreIdAsync(int storeId)
        {
            return await _context.Products
                .Where(p => p.StoreId == storeId)
                .CountAsync();
        }

        public async Task<ProductDetailResponse?> GetDetailAsync(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)   // ✅ để lấy CategoryName
                .Include(p => p.Store)      // ✅ để lấy StoreName
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return null;

            // Nhóm thuộc tính + values
            var variantGroups = await _context.Variants
                .AsNoTracking()
                .Where(v => v.ProductId == productId)
                .Select(v => new VariantGroupDto
                {
                    VariantId = v.Id,
                    VariantName = v.VariantName,
                    Type = v.Type,
                    Values = _context.VariantValues
                        .Where(val => val.VariantId == v.Id)
                        .Select(val => new VariantValueDto
                        {
                            Id = val.Id,
                            ValueName = val.ValueName,
                            Type = val.Type,
                            Image = val.Image,
                            ColorHex = val.colorHex
                        })
                        .ToList()
                })
                .ToListAsync();

            // Biến thể + map valueIds (lọc null -> int)
            var variantRows = await _context.ProductVariants
                .AsNoTracking()
                .Where(pv => pv.ProductId == productId)
                .Select(pv => new
                {
                    pv.Id,
                    pv.Price,
                    pv.CostPrice,
                    pv.Quantity,
                    pv.Weight,
                    pv.Height,
                    pv.Width,
                    pv.Length,
                    pv.Image,
                    VariantValueIds = pv.VariantCompositions
                        .Where(vc => vc.VariantValueId.HasValue)
                        .Select(vc => vc.VariantValueId.Value)
                        .ToList()
                })
                .ToListAsync();

            // Map id -> tên value
            var allValueIds = variantRows.SelectMany(r => r.VariantValueIds).Distinct().ToList();
            var valueNameMap = await _context.VariantValues
                .AsNoTracking()
                .Where(v => allValueIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.ValueName);

            // Build DTO
            var resp = new ProductDetailResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                StoreId = product.StoreId,
                Name = product.Name,
                Brand = product.Brand,
                Weight = product.Weight,
                Slug = product.Slug,
                Description = product.Description,
                MainImage = product.MainImage,
                Status = product.Status.ToString(),
                Quantity = product.Quantity,
                Views = product.Views,
                Rating = product.Rating,
                CreateAt = product.CreateAt,
                UpdateAt = product.UpdateAt,
                CostPrice = product.CostPrice,
                Height = product.Height,
                Width = product.Width,
                Length = product.Length,
                PlaceOfOrigin = product.PlaceOfOrigin,
                Hashtag = product.Hashtag,

                // ✅ tên thay vì id trơ trọi (frontend vẫn nhận cả 2)
                CategoryName = product.Category?.Name,
                StoreName = product.Store?.Name,

                Images = string.IsNullOrWhiteSpace(product.MainImage)
                    ? new List<string>()
                    : new List<string> { product.MainImage },
                VariantGroups = variantGroups
            };

            resp.Variants = variantRows.Select(r => new ProductVariantDto
            {
                Id = r.Id,
                Price = r.Price,
                CostPrice = r.CostPrice,
                Quantity = r.Quantity,
                Weight = r.Weight,
                Height = r.Height,
                Width = r.Width,
                Length = r.Length,
                Image = r.Image,
                Images = new List<string>(),
                VariantValueIds = r.VariantValueIds,
                VariantValueNames = r.VariantValueIds
                    .Select(id => valueNameMap.TryGetValue(id, out var name) ? name : $"#{id}")
                    .ToList()
            }).ToList();

            return resp;
        }
    }
}
