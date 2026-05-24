namespace Application.Grades.Models;

public sealed class GradeScaleDto
{
    public required Guid Id { get; init; }
    public required Guid CourseId { get; init; }
    public required decimal MinPoints { get; init; }
    public required decimal MaxPoints { get; init; }
    public required string Grade { get; init; }
}
