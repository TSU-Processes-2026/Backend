namespace Application.Reviews.Models;

public sealed class FinalGradeResponse
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public decimal? FinalScore { get; set; }
    public string? FinalSource { get; set; }
    public string? TeacherOverrideComment { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
}
