using DATN_API.Data;
using DATN_API.Interfaces;
using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DATN_API.Services
{
    public class DecoratesService : IDecoratesService
    {
        private readonly ApplicationDbContext _context;

        public DecoratesService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả đối tượng Decorates, bao gồm User và AdminSettings
        public async Task<IEnumerable<Decorates>> GetAllAsync()
        {
            return await _context.Decorates
                                 .Include(d => d.User)
                                 .Include(d => d.AdminSetting)
                                 .ToListAsync();
        }

        // Lấy đối tượng Decorates theo Id, bao gồm User và AdminSettings
        public async Task<Decorates> GetByIdAsync(int id)
        {
            return await _context.Decorates
                                 .Include(d => d.User)
                                 .Include(d => d.AdminSetting)
                                 .FirstOrDefaultAsync(d => d.Id == id);
        }

        // Tạo một đối tượng Decorates mới với tất cả các thuộc tính cần thiết
        public async Task<Decorates> CreateAsync(Decorates model)
        {
            _context.Decorates.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        // Cập nhật đối tượng Decorates đã có, bao gồm các thuộc tính mới cho các slide và ảnh decorate
        public async Task<bool> UpdateAsync(int id, Decorates model)
        {
            if (id != model.Id) return false;

            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return false;

            // 🎯 Cập nhật lại toàn bộ các thuộc tính
            decorate.UserId = model.UserId;
            decorate.AdminSettingId = model.AdminSettingId;
            decorate.Video = model.Video;

            // 🎯 Cập nhật các slide
            decorate.Slide1 = model.Slide1;
            decorate.TitleSlide1 = model.TitleSlide1;
            decorate.DescriptionSlide1 = model.DescriptionSlide1;

            decorate.Slide2 = model.Slide2;
            decorate.TitleSlide2 = model.TitleSlide2;
            decorate.DescriptionSlide2 = model.DescriptionSlide2;

            decorate.Slide3 = model.Slide3;
            decorate.TitleSlide3 = model.TitleSlide3;
            decorate.DescriptionSlide3 = model.DescriptionSlide3;

            // 🎯 Cập nhật Decorate ảnh 1
            decorate.Image1 = model.Image1;
            decorate.Title1 = model.Title1;
            decorate.Description1 = model.Description1;

            // 🎯 Cập nhật Decorate ảnh 2
            decorate.Image2 = model.Image2;
            decorate.Title2 = model.Title2;
            decorate.Description2 = model.Description2;

            await _context.SaveChangesAsync();
            return true;
        }

        // Xóa một đối tượng Decorates theo Id
        public async Task<bool> DeleteAsync(int id)
        {
            var decorate = await _context.Decorates.FindAsync(id);
            if (decorate == null) return false;

            _context.Decorates.Remove(decorate);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}