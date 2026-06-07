namespace Infrastructure.Persistence.Entities;

public sealed class ReviewAssignment
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public Guid? ReviewerTeamId { get; set; }
    public required string ReviewTargetType { get; set; } = "submission";
    public required string Status { get; set; } = "pending";
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    
    public Post Task { get; set; } = null!;
    public Submission Submission { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
