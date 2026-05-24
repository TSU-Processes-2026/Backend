namespace Application.Grades.Models;

public sealed class UpsertGradeScaleRequest
{
    public IReadOnlyList<GradeScaleRangeRequest> Ranges { get; init; } = Array.Empty<GradeScaleRangeRequest>();
}
