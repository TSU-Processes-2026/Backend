using System.ComponentModel.DataAnnotations;

namespace Application.Teams.Models;

public sealed class CaptainVoteRequest
{
    [Required]
    public Guid VotedForUserId { get; set; }
}
