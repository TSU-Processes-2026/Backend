namespace Infrastructure.Persistence.Entities;

public sealed class AuthSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public required ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
