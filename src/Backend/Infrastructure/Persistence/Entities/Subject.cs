namespace Infrastructure.Persistence.Entities;

public sealed class Subject
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string GradingMode { get; set; } = "five_point";
    public required ICollection<SubjectParticipant> Participants { get; set; } = new List<SubjectParticipant>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<GradeScale> GradeScales { get; set; } = new List<GradeScale>();
}
