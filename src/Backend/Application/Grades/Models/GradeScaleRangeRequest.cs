namespace Application.Grades.Models;

public sealed class GradeScaleRangeRequest
{
    public decimal MinPoints { get; init; }
    public decimal MaxPoints { get; init; }
    public string? Grade { get; init; }
}
