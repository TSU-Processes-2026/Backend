namespace Infrastructure.Persistence.Entities;

public sealed class GradeScale
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public required string Grade { get; set; }
    public Subject Subject { get; set; } = null!;
}
