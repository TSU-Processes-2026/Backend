namespace Application.Grades.Models;

public sealed class CourseGradeDto
{
    public required Guid Id { get; init; }
    public required Guid CourseId { get; init; }
    public required Guid StudentId { get; init; }
    public required decimal FinalScore { get; init; }
    public required string FinalGrade { get; init; }
    public required DateTimeOffset CalculatedAt { get; init; }
}
