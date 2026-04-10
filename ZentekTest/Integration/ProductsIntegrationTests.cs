using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using ZentekAPI.Models;

namespace ZentekAPI.Integration;

public class ProductsIntegrationTests : IClassFixture<ProductsApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public ProductsIntegrationTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private async Task<string> GetTokenAsync(string user = "admin", string pass = "admin123")
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login", new { Username = user, Password = pass });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    private void SetBearerToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Health ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_IsAnonymous_Returns200()
    {
        var resp = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsExpectedShape()
    {
        var resp = await _client.GetAsync("/api/health");
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("OK", body.GetProperty("status").GetString());
        Assert.Equal("Products API", body.GetProperty("service").GetString());
    }

    // ── Auth ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Username = "admin", Password = "admin123" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("token").GetString()?.Length > 10);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Username = "admin", Password = "wrongpassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── GET /api/products ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var resp = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithValidToken_Returns200()
    {
        SetBearerToken(await GetTokenAsync());

        var resp = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GetProducts_ReturnsJsonArray()
    {
        SetBearerToken(await GetTokenAsync());

        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/api/products", _json);

        Assert.NotNull(products);
        Assert.IsType<List<ProductResponse>>(products);
    }

    [Fact]
    public async Task GetProducts_WithColourFilter_ReturnsOnlyThatColour()
    {
        var token = await GetTokenAsync();
        SetBearerToken(token);

        // Create a distinctly-coloured product
        await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Purple Widget", Description = "desc", Colour = "Purple",
            Price = 5.99, StockQuantity = 10
        });

        var products = await _client.GetFromJsonAsync<List<ProductResponse>>(
            "/api/products?colour=Purple", _json);

        Assert.NotNull(products);
        Assert.All(products, p => Assert.Equal("Purple", p.Colour));
    }

    // ── POST /api/products ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var resp = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Test", Description = "d", Colour = "Red", Price = 1.0, StockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ValidPayload_Returns201()
    {
        SetBearerToken(await GetTokenAsync());

        var resp = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "New Gadget", Description = "A gadget", Colour = "Black",
            Price = 49.99, StockQuantity = 25
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ResponseContainsId()
    {
        SetBearerToken(await GetTokenAsync());

        var resp = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Gadget With ID", Description = "desc", Colour = "Orange",
            Price = 9.99, StockQuantity = 5
        });

        var body = await resp.Content.ReadFromJsonAsync<ProductResponse>(_json);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task CreateProduct_EmptyName_Returns400()
    {
        SetBearerToken(await GetTokenAsync());

        var resp = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "", Description = "d", Colour = "Red", Price = 1.0, StockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_NegativePrice_Returns400()
    {
        SetBearerToken(await GetTokenAsync());

        var resp = await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Bad", Description = "d", Colour = "Red", Price = -5.0, StockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ThenAppearsInList()
    {
        SetBearerToken(await GetTokenAsync());
        var uniqueColour = "Teal_" + Guid.NewGuid().ToString("N")[..6];

        await _client.PostAsJsonAsync("/api/products", new
        {
            Name = "Teal Mug", Description = "desc", Colour = uniqueColour,
            Price = 12.50, StockQuantity = 7
        });

        var products = await _client.GetFromJsonAsync<List<ProductResponse>>(
            $"/api/products?colour={uniqueColour}", _json);

        Assert.Single(products!);
        Assert.Equal("Teal Mug", products![0].Name);
    }
}
