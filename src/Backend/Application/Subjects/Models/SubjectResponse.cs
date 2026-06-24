namespace Application.Subjects.Models;

public sealed class SubjectResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string GradingMode { get; init; }
    public required bool SelfAssessmentEnabled { get; init; }
    public Guid? FinalGradeScaleId { get; init; }
    public required bool PeerReviewEnabled { get; init; }
    public string? PeerReviewScope { get; init; }
    public string? PeerReviewMode { get; init; }
    public string? PeerReviewDeadlinePolicy { get; init; }
    public string? TeacherFinalMode { get; init; }
    public string? PairingStrategy { get; init; }
    public required bool ShowCriteriaBeforeDeadline { get; init; }
    public required bool LiveReviewMode { get; init; }
    public int? DefaultReviewTimeLimitMinutes { get; init; }
}
