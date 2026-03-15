using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.TestHost;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly string _databaseName;

    public ApiWebApplicationFactory()
    {
        _databaseName = $"lms-tests-{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable("Database__Provider", "InMemory");
        Environment.SetEnvironmentVariable("Database__InMemoryName", _databaseName);
        Environment.SetEnvironmentVariable("FileStorage__RootPath", Path.Combine(Path.GetTempPath(), _databaseName, "uploads"));
        Environment.SetEnvironmentVariable("Jwt__Issuer", "lms-test-issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "lms-test-audience");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "01234567890123456789012345678901");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenLifetimeSeconds", "900");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenLifetimeSeconds", "604800");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        });
    }
}
