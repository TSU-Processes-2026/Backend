namespace Application.Grades.Models;

public sealed class CourseAnalyticsResponse
{
    public Guid CourseId { get; set; }
    public List<StudentAnalyticsRow> Rows { get; set; } = new();
    public List<string> TaskTitles { get; set; } = new();
}
