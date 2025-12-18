using Microsoft.EntityFrameworkCore;
using Artifex_Backend_2.Models;

namespace Artifex_Backend_2.Data
{
    public class ArtifexDbContext: DbContext
    {
        public ArtifexDbContext(DbContextOptions<ArtifexDbContext> options) : base(options)
        {}

        public DbSet<User> Users { get; set; }
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasOne(u => u.SellerProfile)
                .WithOne(s => s.User)
                .HasForeignKey<Seller>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasOne(u => u.CustomerProfile)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Product>()
                .HasOne<Seller>()
                .WithMany()
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
