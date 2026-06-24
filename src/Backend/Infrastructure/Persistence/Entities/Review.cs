namespace Infrastructure.Persistence.Entities;

public sealed class Review
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public required string Source { get; set; } = "peer";
    public decimal? OverallScore { get; set; }
    public string? OverallComment { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public bool IsFinal { get; set; }
    public bool IsRejected { get; set; }
    public bool ReplacedByTeacher { get; set; }
    
    public ReviewAssignment Assignment { get; set; } = null!;
    public ICollection<CriterionResult> CriterionResults { get; set; } = new List<CriterionResult>();
}
