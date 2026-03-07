namespace Application.Auth.Models;

public sealed class TokenResponse
{
    public required string TokenType { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }
    public required int RefreshExpiresIn { get; init; }
    public required Guid UserId { get; init; }
    public required Guid SessionId { get; init; }
}
