namespace Application.Grades.Models;

public sealed class TaskGradeCell
{
    public Guid TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public string? Source { get; set; }
    public int ReviewerCount { get; set; }
}
