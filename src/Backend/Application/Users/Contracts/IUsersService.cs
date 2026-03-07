using Application.Auth.Models;

namespace Application.Users.Contracts;

public interface IUsersService
{
    Task<UserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
