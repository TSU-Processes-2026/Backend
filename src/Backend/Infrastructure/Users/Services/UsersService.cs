using Application.Auth.Models;
using Application.Users.Contracts;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Users.Services;

public sealed class UsersService : IUsersService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || string.IsNullOrWhiteSpace(user.UserName))
        {
            return null;
        }

        return new UserResponse
        {
            Id = user.Id,
            Username = user.UserName
        };
    }
}
