namespace Infrastructure.Persistence.Entities;

public sealed class Subject
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string GradingMode { get; set; } = "five_point";
    public bool SelfAssessmentEnabled { get; set; }
    public Guid? FinalGradeScaleId { get; set; }
    public bool PeerReviewEnabled { get; set; }
    public string? PeerReviewScope { get; set; }
    public string? PeerReviewMode { get; set; }
    public string? PeerReviewDeadlinePolicy { get; set; }
    public string? TeacherFinalMode { get; set; }
    public string? PairingStrategy { get; set; }
    public bool ShowCriteriaBeforeDeadline { get; set; }
    public bool LiveReviewMode { get; set; }
    public int? DefaultReviewTimeLimitMinutes { get; set; }
    public required ICollection<SubjectParticipant> Participants { get; set; } = new List<SubjectParticipant>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<GradeScale> GradeScales { get; set; } = new List<GradeScale>();
}
