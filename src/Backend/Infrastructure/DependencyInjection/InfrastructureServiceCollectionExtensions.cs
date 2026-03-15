using Application.Assignments.Contracts;
using Application.Auth.Contracts;
using Application.Comments.Contracts;
using Application.Auth.Options;
using Application.Posts.Contracts;
using Application.Subjects.Contracts;
using Application.Users.Contracts;
using Infrastructure.Assignments.Services;
using Infrastructure.Auth.Services;
using Infrastructure.Comments.Services;
using Infrastructure.Files.Contracts;
using Infrastructure.Files.Options;
using Infrastructure.Files.Services;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Posts.Services;
using Infrastructure.Subjects.Services;
using Infrastructure.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"];

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters long.")
            .ValidateOnStart();

        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .ValidateOnStart();

        if (string.Equals(databaseProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            var databaseName = configuration["Database:InMemoryName"] ?? "lms-tests";
            services.AddDbContext<LmsDbContext>(options => options.UseInMemoryDatabase(databaseName));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
            services.AddDbContext<LmsDbContext>(options => options.UseNpgsql(connectionString));
        }

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<LmsDbContext>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICommentsService, CommentsService>();
        services.AddScoped<ISubjectsService, SubjectsService>();
        services.AddScoped<IPostsService, PostsService>();
        services.AddScoped<IAssignmentsService, AssignmentsService>();
        services.AddScoped<IUsersService, UsersService>();

        return services;
    }
}
