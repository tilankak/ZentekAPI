using Microsoft.EntityFrameworkCore;
using ZentekAPI.Data;
using ZentekAPI.Models;

namespace ZentekAPI.Services;

public interface IProductService
{
    Task<IEnumerable<ProductResponse>> GetAllProductsAsync(string? colour = null);
    Task<ProductResponse?> GetProductByIdAsync(Guid id);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);
}

public class ProductService : IProductService
{
    private readonly ProductsDbContext _context;

    public ProductService(ProductsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductResponse>> GetAllProductsAsync(string? colour = null)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(colour))
        {
            query = query.Where(p => p.Colour.ToLower() == colour.ToLower());
        }

        return await query
            .OrderBy(p => p.CreatedAt)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        return product is null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Colour = request.Colour,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    private static ProductResponse MapToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Colour, p.Price, p.StockQuantity, p.CreatedAt, p.UpdatedAt);
}
