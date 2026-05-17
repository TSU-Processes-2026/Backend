namespace Application.Submissions.Models;

public sealed class SubmissionWithSelfAssessmentRequest
{
    public string? Content { get; init; }
    public IReadOnlyList<SelfAssessmentRequest>? SelfAssessments { get; init; }
}
