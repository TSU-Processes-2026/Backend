namespace Application.Subjects.Models;

public sealed class UpdateSubjectRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? GradingMode { get; init; }
    public bool? SelfAssessmentEnabled { get; init; }
    public Guid? FinalGradeScaleId { get; init; }
    public bool? PeerReviewEnabled { get; init; }
    public string? PeerReviewScope { get; init; }
    public string? PeerReviewMode { get; init; }
    public string? PeerReviewDeadlinePolicy { get; init; }
    public string? TeacherFinalMode { get; init; }
    public string? PairingStrategy { get; init; }
    public bool? ShowCriteriaBeforeDeadline { get; init; }
    public bool? LiveReviewMode { get; init; }
    public int? DefaultReviewTimeLimitMinutes { get; init; }
}
