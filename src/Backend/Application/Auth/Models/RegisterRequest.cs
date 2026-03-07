using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Models;

public sealed class RegisterRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public required string Username { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }
}
