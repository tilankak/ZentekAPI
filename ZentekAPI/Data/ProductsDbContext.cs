using Microsoft.EntityFrameworkCore;
using ZentekAPI.Models;

namespace ZentekAPI.Data;

public class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(1000);
            entity.Property(p => p.Colour).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        // Seed some data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Classic Red Sneaker", Description = "Timeless red sneaker for everyday wear", Colour = "Red", Price = 89.99m, StockQuantity = 50, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Product { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Ocean Blue Backpack", Description = "Durable backpack in ocean blue", Colour = "Blue", Price = 49.99m, StockQuantity = 120, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Product { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Forest Green Jacket", Description = "Waterproof jacket in forest green", Colour = "Green", Price = 149.99m, StockQuantity = 30, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
    }
}
