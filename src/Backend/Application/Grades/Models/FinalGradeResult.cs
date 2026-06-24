namespace Application.Grades.Models;

public sealed class FinalGradeResult
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public decimal? FinalScore { get; set; }
    public string? FinalSource { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
}
