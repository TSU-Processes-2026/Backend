namespace Application.Grades.Models;

public sealed class StudentAnalyticsRow
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<TaskGradeCell> TaskGrades { get; set; } = new();
    public decimal? FinalCourseGrade { get; set; }
}
