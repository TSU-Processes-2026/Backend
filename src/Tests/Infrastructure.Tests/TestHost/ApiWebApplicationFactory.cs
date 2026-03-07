using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.Tests.TestHost;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<global::Program>, IDisposable
{
    private readonly SqliteConnection _connection;

    public ApiWebApplicationFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=lms_test;Username=lms;Password=lms",
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
            services.RemoveAll<DbContextOptions<LmsDbContext>>();
            services.RemoveAll<LmsDbContext>();
            services.RemoveAll<SqliteConnection>();

            services.AddSingleton(_connection);
            services.AddDbContext<LmsDbContext>((serviceProvider, optionsBuilder) =>
            {
                optionsBuilder.UseSqlite(serviceProvider.GetRequiredService<SqliteConnection>());
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LmsDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public new void Dispose()
    {
        _connection.Dispose();
        base.Dispose();
    }
}
