using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Models;

public sealed class RefreshTokenRequest
{
    [Required]
    [MinLength(1)]
    public required string RefreshToken { get; init; }
}
