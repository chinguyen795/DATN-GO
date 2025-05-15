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
        public DbSet<Diners> Diners { get; set; }
        public DbSet<Reports> Reports { get; set; }
        public DbSet<ReportActions> ReportActions { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Options> Options { get; set; }
        public DbSet<OptionValues> OptionValues { get; set; }
        public DbSet<ProductSkus> ProductSkus { get; set; }
        public DbSet<SkusValues> SkusValues { get; set; }
        public DbSet<ShippingMethods> ShippingMethods { get; set; }
        public DbSet<Vouchers> Vouchers { get; set; }
        public DbSet<ProductVouchers> ProductVouchers { get; set; }
        public DbSet<Carts> Carts { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<Reviews> Reviews { get; set; }
        public DbSet<DeliveryTrackings> DeliveryTrackings { get; set; }
        public DbSet<OrderDetails> OrderDetails { get; set; }


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
    .HasOne(u => u.Diner)
    .WithOne(d => d.User)
    .HasForeignKey<Diners>(d => d.UserId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reports>()
    .HasOne(r => r.User)
    .WithMany(u => u.ReportsSent)
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reports>()
                .HasOne(r => r.Diner)
                .WithMany(d => d.ReportsReceived)
                .HasForeignKey(r => r.DinerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReportActions>()
    .HasOne(ra => ra.Report)
    .WithMany(r => r.Actions)
    .HasForeignKey(ra => ra.ReportId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReportActions>()
                .HasOne(ra => ra.User)
                .WithMany(u => u.ReportActionsPerformed)
                .HasForeignKey(ra => ra.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Products>()
    .HasOne(p => p.Category)
    .WithMany(c => c.Products)
    .HasForeignKey(p => p.CategoryId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Products>()
                .HasOne(p => p.Diner)
                .WithMany(d => d.Products)
                .HasForeignKey(p => p.DinerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - Option
            modelBuilder.Entity<Options>()
                .HasOne(o => o.Product)
                .WithMany(p => p.Options)
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Option - OptionValue
            modelBuilder.Entity<OptionValues>()
                .HasOne(ov => ov.Option)
                .WithMany(o => o.OptionValues)
                .HasForeignKey(ov => ov.OptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - OptionValue
            modelBuilder.Entity<OptionValues>()
                .HasOne(ov => ov.Product)
                .WithMany(p => p.OptionValues)
                .HasForeignKey(ov => ov.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product - ProductSku
            modelBuilder.Entity<ProductSkus>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSkus)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // SkusValue – nhiều khóa ngoại
            modelBuilder.Entity<SkusValues>()
                .HasKey(sv => new { sv.ValueId, sv.SkuId, sv.ProductId, sv.OptionId });

            modelBuilder.Entity<SkusValues>()
                .HasOne(sv => sv.OptionValue)
                .WithMany(ov => ov.SkusValues)
                .HasForeignKey(sv => sv.ValueId);

            modelBuilder.Entity<SkusValues>()
                .HasOne(sv => sv.ProductSku)
                .WithMany(ps => ps.SkusValues)
                .HasForeignKey(sv => sv.SkuId);

            modelBuilder.Entity<SkusValues>()
                .HasOne(sv => sv.Product)
                .WithMany(p => p.SkusValues)
                .HasForeignKey(sv => sv.ProductId);

            modelBuilder.Entity<SkusValues>()
                .HasOne(sv => sv.Option)
                .WithMany(o => o.SkusValues)
                .HasForeignKey(sv => sv.OptionId);

            modelBuilder.Entity<ProductVouchers>()
    .HasKey(pv => new { pv.ProductId, pv.VoucherId });

            modelBuilder.Entity<ProductVouchers>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVouchers)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVouchers>()
                .HasOne(pv => pv.Voucher)
                .WithMany(v => v.ProductVouchers)
                .HasForeignKey(pv => pv.VoucherId)
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

            modelBuilder.Entity<ShippingMethods>()
    .HasOne(sm => sm.Diner)
    .WithMany(d => d.ShippingMethods)
    .HasForeignKey(sm => sm.DinerId)
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

            modelBuilder.Entity<Reviews>()
                .HasOne(r => r.Order)
                .WithMany(o => o.Reviews)
                .HasForeignKey(r => r.OrderId)
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
