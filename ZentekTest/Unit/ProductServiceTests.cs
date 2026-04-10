using Microsoft.EntityFrameworkCore;
using ZentekAPI.Data;
using ZentekAPI.Models;

using ZentekAPI.Services;

namespace Products.Tests.Unit;

public class ProductServiceTests
{
    private static ProductsDbContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<ProductsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // fresh DB per test
            .Options;
        var ctx = new ProductsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    // ── GetAllProducts ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllProductsAsync_ReturnsSeededProducts()
    {
        using var ctx = CreateFreshContext();
        var svc = new ProductService(ctx);

        var results = await svc.GetAllProductsAsync();

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithColourFilter_ReturnsOnlyMatchingColour()
    {
        using var ctx = CreateFreshContext();
        ctx.Products.Add(new Product { Name = "Red Hat", Colour = "Red", Price = 10m, StockQuantity = 5 });
        ctx.Products.Add(new Product { Name = "Blue Hat", Colour = "Blue", Price = 10m, StockQuantity = 5 });
        await ctx.SaveChangesAsync();

        var svc = new ProductService(ctx);
        var results = (await svc.GetAllProductsAsync("Red")).ToList();

        Assert.All(results, p => Assert.Equal("Red", p.Colour));
    }

    [Fact]
    public async Task GetAllProductsAsync_ColourFilter_IsCaseInsensitive()
    {
        using var ctx = CreateFreshContext();
        ctx.Products.Add(new Product { Name = "Green Bag", Colour = "Green", Price = 20m, StockQuantity = 3 });
        await ctx.SaveChangesAsync();

        var svc = new ProductService(ctx);
        var results = (await svc.GetAllProductsAsync("green")).ToList();

        Assert.Contains(results, p => p.Name == "Green Bag");
    }

    [Fact]
    public async Task GetAllProductsAsync_NoMatchingColour_ReturnsEmptyList()
    {
        using var ctx = CreateFreshContext();
        var svc = new ProductService(ctx);

        var results = await svc.GetAllProductsAsync("Purple");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllProductsAsync_NullColour_ReturnsAllProducts()
    {
        using var ctx = CreateFreshContext();
        ctx.Products.Add(new Product { Name = "Red Item", Colour = "Red", Price = 5m, StockQuantity = 1 });
        ctx.Products.Add(new Product { Name = "Blue Item", Colour = "Blue", Price = 5m, StockQuantity = 1 });
        await ctx.SaveChangesAsync();

        var svc = new ProductService(ctx);
        var results = (await svc.GetAllProductsAsync(null)).ToList();

        Assert.True(results.Count >= 2);
    }

    // ── GetProductById ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductByIdAsync_ExistingId_ReturnsProduct()
    {
        using var ctx = CreateFreshContext();
        var product = new Product { Name = "Test Product", Colour = "Yellow", Price = 99m, StockQuantity = 10 };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var svc = new ProductService(ctx);
        var result = await svc.GetProductByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal("Yellow", result.Colour);
    }

    [Fact]
    public async Task GetProductByIdAsync_NonExistingId_ReturnsNull()
    {
        using var ctx = CreateFreshContext();
        var svc = new ProductService(ctx);

        var result = await svc.GetProductByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ── CreateProduct ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProductAsync_ValidRequest_ReturnsCreatedProduct()
    {
        using var ctx = CreateFreshContext();
        var svc = new ProductService(ctx);
        var request = new CreateProductRequest("Widget", "A small widget", "Silver", 14.99m, 100);

        var result = await svc.CreateProductAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Widget", result.Name);
        Assert.Equal("Silver", result.Colour);
        Assert.Equal(14.99m, result.Price);
    }

    [Fact]
    public async Task CreateProductAsync_PersistsToDatabase()
    {
        using var ctx = CreateFreshContext();
        var svc = new ProductService(ctx);
        var request = new CreateProductRequest("Persisted Item", "desc", "Black", 1m, 1);

        var result = await svc.CreateProductAsync(request);

        var fromDb = await ctx.Products.FindAsync(result.Id);
        Assert.NotNull(fromDb);
        Assert.Equal("Persisted Item", fromDb.Name);
    }

    [Fact]
    public async Task CreateProductAsync_SetsCreatedAtTimestamp()
    {
        using var ctx = CreateFreshContext();
        var svc = new ProductService(ctx);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await svc.CreateProductAsync(new CreateProductRequest("Timed", "t", "White", 1m, 1));

        Assert.True(result.CreatedAt >= before);
    }
}
