using System.ComponentModel.DataAnnotations;

namespace Application.Teams.Models;

public sealed class AssignCaptainRequest
{
    [Required]
    public Guid CaptainUserId { get; set; }
}
