using System.ComponentModel.DataAnnotations;

namespace Application.Submissions.Models;

public sealed class DecisionVoteRequest
{
    [Required]
    public DecisionType Decision { get; set; }

    public string? Comment { get; set; }
}
