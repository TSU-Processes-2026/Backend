using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Models;

public sealed class LoginRequest
{
    [Required]
    [MinLength(1)]
    public required string Username { get; init; }

    [Required]
    [MinLength(1)]
    public required string Password { get; init; }
}
