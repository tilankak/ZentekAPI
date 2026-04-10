using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZentekAPI;
using ZentekAPI.Data;

namespace ZentekAPI.Integration;

public class ProductsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the real DbContext with a fresh in-memory one per test run
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ProductsDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ProductsDbContext>(opts =>
                opts.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid()));
        });

        builder.UseEnvironment("Development");
    }
}
