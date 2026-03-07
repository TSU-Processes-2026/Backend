using Application.Auth.Models;

namespace Application.Auth.Contracts;

public interface IAuthService
{
    Task<RegisterAuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<LoginAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<RefreshAuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<LogoutAuthResult> LogoutAsync(Guid userId, RefreshTokenRequest request, CancellationToken cancellationToken);
}
