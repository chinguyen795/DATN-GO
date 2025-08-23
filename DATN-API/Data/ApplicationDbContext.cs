using DATN_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Net;
using Twilio.TwiML.Messaging;
using Twilio.TwiML.Voice;

namespace DATN_API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Models.Users> Users { get; set; }
        public DbSet<Models.Roles> Roles { get; set; }
        public DbSet<Addresses> Addresses { get; set; }
        public DbSet<Cities> Cities { get; set; }
        public DbSet<Districts> Districts { get; set; }
        public DbSet<Wards> Wards { get; set; }
        public DbSet<Messages> Messages { get; set; }
        public DbSet<Posts> Posts { get; set; }
        public DbSet<Decorates> Decorates { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Stores> Stores { get; set; }
        public DbSet<Variants> Variants { get; set; }

        public DbSet<VariantValues> VariantValues { get; set; }
        public DbSet<ProductVariants> ProductVariants { get; set; }
        public DbSet<VariantComposition> VariantCompositions { get; set; }
        public DbSet<Prices> Prices { get; set; }
        public DbSet<ProductImages> ProductImages { get; set; }
        public DbSet<MessageMedias> MessageMedias { get; set; }
        public DbSet<ReviewMedias> ReviewMedias { get; set; }
        public DbSet<FollowStores> FollowStores { get; set; }
        public DbSet<RatingStores> RatingStores { get; set; }
        public DbSet<AdminSettings> AdminSettings { get; set; }
        public DbSet<Policies> Policies { get; set; }
        public DbSet<Carts> Carts { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<DeliveryTrackings> DeliveryTrackings { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }
        public DbSet<ShippingMethods> ShippingMethods { get; set; }
        public DbSet<ProductVouchers> ProductVouchers { get; set; }
        public DbSet<Vouchers> Vouchers { get; set; }
        public DbSet<CartItemVariants> CartItemVariants { get; set; }
        public DbSet<UserVouchers> UserVouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasMany(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cities>()
                .HasMany(c => c.Districts)
                .WithOne(d => d.City)
                .HasForeignKey(d => d.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Districts>()
                .HasMany(d => d.Wards)
                .WithOne(w => w.District)
                .HasForeignKey(w => w.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cities>()
                .HasOne(c => c.Address)
                .WithOne(a => a.City)
                .HasForeignKey<Cities>(c => c.Id) // ID của City cũng là khóa ngoại đến Address
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Messages>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Messages>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Posts>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Decorates>()
                .HasOne(d => d.User)
                .WithMany(u => u.Decorates)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.Store)
                .WithOne(s => s.User)
                .HasForeignKey<Stores>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Products>()
                .HasOne(p => p.Store)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Products>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Variants>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VariantValues>()
                .HasOne(vv => vv.Variant)
                .WithMany()
                .HasForeignKey(vv => vv.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariants>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImages>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductImages>()
                .HasOne(pi => pi.ProductVariant)
                .WithMany()
                .HasForeignKey(pi => pi.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VariantComposition>()
                .HasOne(vc => vc.ProductVariant)
                .WithMany()
                .HasForeignKey(vc => vc.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VariantComposition>()
                .HasOne(vc => vc.VariantValue)
                .WithMany()
                .HasForeignKey(vc => vc.VariantValueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VariantComposition>()
                .HasOne(vc => vc.Variant)
                .WithMany()
                .HasForeignKey(vc => vc.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prices>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Prices)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Carts>()
                .HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Carts>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Carts)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Orders>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Orders>()
                .HasOne(o => o.Voucher)
                .WithMany()
                .HasForeignKey(o => o.VoucherId)
                .OnDelete(DeleteBehavior.SetNull); // vì có thể không chọn voucher

            modelBuilder.Entity<Orders>()
                .HasOne(o => o.ShippingMethod)
                .WithMany()
                .HasForeignKey(o => o.ShippingMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reviews>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reviews>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryTrackings>()
                .HasOne(dt => dt.Order)
                .WithOne(o => o.DeliveryTracking)
                .HasForeignKey<DeliveryTrackings>(dt => dt.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetails>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetails>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
