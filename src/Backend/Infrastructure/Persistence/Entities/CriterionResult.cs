namespace Infrastructure.Persistence.Entities;

public sealed class CriterionResult
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid CriterionId { get; set; }
    public decimal? Value { get; set; }
    public string? Comment { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public Submission Submission { get; set; } = null!;
    public Criterion Criterion { get; set; } = null!;
}
