namespace Application.Reviews.Models;

public sealed class OverrideGradeRequest
{
    public decimal FinalScore { get; set; }
    public string? Comment { get; set; }
}
