using Microsoft.AspNetCore.Mvc;
using Moq;
using ZentekAPI.Controllers;
using ZentekAPI.Models;
using ZentekAPI.Services;

namespace Products.Tests.Unit;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _mockService = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_mockService.Object);
    }

    // ── GET /products ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithProducts()
    {
        var products = new List<ProductResponse>
        {
            new(Guid.NewGuid(), "Product A", "Desc A", "Red", 10m, 5, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), "Product B", "Desc B", "Blue", 20m, 3, DateTime.UtcNow, DateTime.UtcNow)
        };
        _mockService.Setup(s => s.GetAllProductsAsync(null)).ReturnsAsync(products);

        var result = await _controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<ProductResponse>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetAll_WithColour_PassesColourToService()
    {
        _mockService.Setup(s => s.GetAllProductsAsync("Green")).ReturnsAsync(new List<ProductResponse>());

        await _controller.GetAll("Green");

        _mockService.Verify(s => s.GetAllProductsAsync("Green"), Times.Once);
    }

    // ── GET /products/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var product = new ProductResponse(id, "P", "D", "Red", 5m, 1, DateTime.UtcNow, DateTime.UtcNow);
        _mockService.Setup(s => s.GetProductByIdAsync(id)).ReturnsAsync(product);

        var result = await _controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(product, ok.Value);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetProductByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProductResponse?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    // ── POST /products ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201Created()
    {
        var request = new CreateProductRequest("Widget", "Desc", "Silver", 9.99m, 50);
        var created = new ProductResponse(Guid.NewGuid(), "Widget", "Desc", "Silver", 9.99m, 50, DateTime.UtcNow, DateTime.UtcNow);
        _mockService.Setup(s => s.CreateProductAsync(request)).ReturnsAsync(created);

        var result = await _controller.Create(request);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdAt.StatusCode);
        Assert.Equal(created, createdAt.Value);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateProductRequest("", "Desc", "Red", 1m, 1);

        var result = await _controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_EmptyColour_ReturnsBadRequest()
    {
        var request = new CreateProductRequest("Name", "Desc", "", 1m, 1);

        var result = await _controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_NegativePrice_ReturnsBadRequest()
    {
        var request = new CreateProductRequest("Name", "Desc", "Red", -1m, 1);

        var result = await _controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
