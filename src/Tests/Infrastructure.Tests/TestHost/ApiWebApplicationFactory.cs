using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.TestHost;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly string _databaseName;

    public ApiWebApplicationFactory()
    {
        _databaseName = $"lms-tests-{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Database:InMemoryName"] = _databaseName,
                ["Jwt:Issuer"] = "lms-test-issuer",
                ["Jwt:Audience"] = "lms-test-audience",
                ["Jwt:SigningKey"] = "01234567890123456789012345678901",
                ["Jwt:AccessTokenLifetimeSeconds"] = "900",
                ["Jwt:RefreshTokenLifetimeSeconds"] = "604800"
            };

            configurationBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}
