using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using DATN_API.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class ReviewsService : IReviewsService
    {
        private readonly ApplicationDbContext _context;
        public ReviewsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Reviews>> GetAllAsync()
        {
            return await _context.Reviews
                .Include(r => r.ReviewMedias)
                .ToListAsync();
        }

        public async Task<Reviews?> GetByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.ReviewMedias)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Reviews> CreateAsync(Reviews model, List<string>? mediaList)
        {
            // Kiểm tra đơn hàng
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order == null || order.Status != OrderStatus.DaHoanThanh)
                throw new Exception("Đơn hàng chưa hoàn thành hoặc không tồn tại");

            // Kiểm tra sản phẩm trong đơn
            var productInOrder = order.OrderDetails.Any(od => od.ProductId == model.ProductId);
            if (!productInOrder)
                throw new Exception("Sản phẩm không thuộc đơn hàng này");

            // Kiểm tra review trùng
            var existingReview = await _context.Reviews
     .FirstOrDefaultAsync(r => r.OrderId == model.OrderId
                            && r.ProductId == model.ProductId
                            && r.UserId == model.UserId);
            if (existingReview != null)
                throw new Exception("Bạn đã đánh giá sản phẩm này trong đơn hàng này");


            model.CreateAt = DateTime.Now;
            model.UpdateAt = DateTime.Now;

            _context.Reviews.Add(model);
            await _context.SaveChangesAsync();

            // Thêm media
            if (mediaList != null && mediaList.Count > 0)
            {
                if (mediaList.Count > 3)
                    throw new Exception("Chỉ được upload tối đa 3 ảnh");

                foreach (var media in mediaList)
                {
                    _context.ReviewMedias.Add(new ReviewMedias
                    {
                        ReviewId = model.Id,
                        Media = media
                    });
                }
                await _context.SaveChangesAsync();
            }

            return model;
        }

        public async Task<IEnumerable<ReviewViewModel>> GetByProductIdAsync(int productId)
        {
            var purchaseCount = await _context.OrderDetails
       .Where(od => od.ProductId == productId
                 && od.Order.Status == OrderStatus.DaHoanThanh)
       .CountAsync();

            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.ReviewMedias)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreateAt)
                .ToListAsync();

            return reviews.Select(r => new ReviewViewModel
            {
                ReviewId = r.Id,
                UserId = r.UserId,
                UserName = r.User?.FullName,
                AvatarUrl = r.User?.Avatar,
                Rating = r.Rating,
                CommentText = r.CommentText,
                CreatedDate = r.CreateAt,
                MediaUrls = r.ReviewMedias?.Select(m => m.Media).ToList(),
                OrderId = r.OrderId,
                PurchaseCount = purchaseCount
            });
        }




        public async Task<bool> UpdateAsync(int id, Reviews model, List<string>? mediaList)
        {
            var entity = await _context.Reviews
                .Include(r => r.ReviewMedias)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null) return false;

            entity.Rating = model.Rating;
            entity.CommentText = model.CommentText;
            entity.UpdateAt = DateTime.Now;

            // Xóa media cũ
            _context.ReviewMedias.RemoveRange(entity.ReviewMedias);

            // Thêm media mới
            if (mediaList != null && mediaList.Count > 0)
            {
                if (mediaList.Count > 3)
                    throw new Exception("Chỉ được upload tối đa 3 ảnh");

                foreach (var media in mediaList)
                {
                    _context.ReviewMedias.Add(new ReviewMedias
                    {
                        ReviewId = entity.Id,
                        Media = media
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Reviews.FindAsync(id);
            if (entity == null) return false;

            _context.Reviews.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HasUserReviewedProductAsync(int orderId, int productId, int userId)
        {
            return await _context.Reviews
                .AnyAsync(r => r.OrderId == orderId
                            && r.ProductId == productId
                            && r.UserId == userId);
        }


        public async Task<bool> IsOrderCompletedAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            return order.Status == OrderStatus.DaHoanThanh;
        }

        public async Task<List<CompletedOrderViewModel>> GetCompletedOrdersByUserAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.DaHoanThanh)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Select(o => new CompletedOrderViewModel
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status.ToString(),
                    Products = o.OrderDetails.Select(od => new CompletedOrderProduct
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product.Name
                    }).ToList()
                })
                .ToListAsync();
        }


    }
}