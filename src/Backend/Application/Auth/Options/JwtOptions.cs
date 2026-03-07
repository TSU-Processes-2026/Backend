using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(1)]
    public required string Issuer { get; init; }

    [Required]
    [MinLength(1)]
    public required string Audience { get; init; }

    [Required]
    [MinLength(32)]
    public required string SigningKey { get; init; }

    [Range(1, int.MaxValue)]
    public int AccessTokenLifetimeSeconds { get; init; } = 900;

    [Range(1, int.MaxValue)]
    public int RefreshTokenLifetimeSeconds { get; init; } = 604800;
}
