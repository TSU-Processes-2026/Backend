using Infrastructure.Identity;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        
        var adminEmail = "admin@ad.min";
        var adminPassword = "admin123";

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = provider.GetRequiredService<LmsDbContext>();

        const string adminRoleName = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = adminRoleName,
                NormalizedName = adminRoleName.ToUpperInvariant()
            };

            var roleResult = await roleManager.CreateAsync(role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role {adminRoleName}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }

        var existingUser = await userManager.FindByEmailAsync(adminEmail);

        if (existingUser == null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                NormalizedUserName = adminEmail.ToUpperInvariant(),
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("D")
            };

            var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createUserResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
            }

            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add admin user to role {adminRoleName}: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
            }

            var subjects = await db.Subjects.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var subject in subjects)
            {
                var exists = await db.SubjectParticipants
                    .AnyAsync(x => x.SubjectId == subject.Id && x.UserId == adminUser.Id, cancellationToken);

                if (!exists)
                {
                    db.SubjectParticipants.Add(new SubjectParticipant
                    {
                        Subject = null!,
                        SubjectId = subject.Id,
                        UserId = adminUser.Id,
                        Role = adminRoleName
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var isInRole = await userManager.IsInRoleAsync(existingUser, adminRoleName);
        if (!isInRole)
        {
            var addToRoleResult = await userManager.AddToRoleAsync(existingUser, adminRoleName);
            if (!addToRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add existing user to role {adminRoleName}: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        var subjectList = await db.Subjects.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var subject in subjectList)
        {
            var exists = await db.SubjectParticipants
                .AnyAsync(x => x.SubjectId == subject.Id && x.UserId == existingUser.Id, cancellationToken);

            if (!exists)
            {
                db.SubjectParticipants.Add(new SubjectParticipant
                {
                    Subject = null!,
                    SubjectId = subject.Id,
                    UserId = existingUser.Id,
                    Role = adminRoleName
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}