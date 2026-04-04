using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Souq.Models;

namespace Souq.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<VendorProfile> VendorProfiles => Set<VendorProfile>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductVariation> ProductVariations => Set<ProductVariation>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<VendorPayout> VendorPayouts => Set<VendorPayout>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── GLOBAL: disable all cascade deletes ──────────────────
            foreach (var relationship in builder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // ── Decimal precision ─────────────────────────────────────
            builder.Entity<Product>()
                .Property(p => p.BasePrice).HasPrecision(18, 2);
            builder.Entity<ProductVariation>()
                .Property(p => p.Price).HasPrecision(18, 2);
            builder.Entity<Order>()
                .Property(o => o.SubTotal).HasPrecision(18, 2);
            builder.Entity<Order>()
                .Property(o => o.PlatformFee).HasPrecision(18, 2);
            builder.Entity<Order>()
                .Property(o => o.Total).HasPrecision(18, 2);
            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            builder.Entity<OrderItem>()
                .Property(oi => oi.VendorEarnings).HasPrecision(18, 2);
            builder.Entity<VendorPayout>()
                .Property(vp => vp.Amount).HasPrecision(18, 2);

            // ── Unique indexes ────────────────────────────────────────
            builder.Entity<Product>()
                .HasIndex(p => p.Slug).IsUnique();
            builder.Entity<Department>()
                .HasIndex(d => d.Slug).IsUnique();
            builder.Entity<Category>()
                .HasIndex(c => c.Slug).IsUnique();
            builder.Entity<VendorProfile>()
                .HasIndex(v => v.StoreSlug).IsUnique();
            builder.Entity<VendorProfile>()
                .HasIndex(v => v.UserId).IsUnique();
        }
    }
}
