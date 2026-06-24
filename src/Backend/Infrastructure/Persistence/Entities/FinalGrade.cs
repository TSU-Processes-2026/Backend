namespace Infrastructure.Persistence.Entities;

public sealed class FinalGrade
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? TeamId { get; set; }
    public decimal? FinalScore { get; set; }
    public string? FinalGradeString { get; set; }
    public string? FinalSource { get; set; }
    public string? TeacherOverrideComment { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
    public Submission Submission { get; set; } = null!;
}
