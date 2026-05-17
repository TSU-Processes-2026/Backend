namespace Infrastructure.Persistence.Entities;

public sealed class CourseGrade
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public decimal FinalScore { get; set; }
    public string FinalGrade { get; set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; set; }
    public Subject Course { get; set; } = null!;
}
