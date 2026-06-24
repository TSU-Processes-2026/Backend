namespace Infrastructure.Persistence.Entities;

public sealed class Post
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AuthorId { get; set; }
    public required string PostType { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? FileName { get; set; }
    public string? StoragePath { get; set; }
    public long? FileSize { get; set; }
    public string? AssignmentData { get; set; }
    public decimal? MaxPoints { get; set; }
    public bool? SelfAssessmentEnabled { get; set; }
    public DateTimeOffset? SelfAssessmentVisibilityDate { get; set; }
    public DateTimeOffset? DeadLine { get; set; }
    public bool ReviewEnabled { get; set; }
    public string? ReviewType { get; set; }
    public string? ReviewMode { get; set; }
    public DateTimeOffset? ReviewDeadlineAt { get; set; }
    public int? ReviewTimeLimitMinutes { get; set; }
    public DateTimeOffset? CriteriaVisibilityAt { get; set; }
    public bool TeacherCanEditPeerScores { get; set; }
    public string? TeamReviewPolicy { get; set; }
    public required Subject Subject { get; set; }
    public ICollection<AssignmentQuestion> Questions { get; set; } = new List<AssignmentQuestion>();
}
