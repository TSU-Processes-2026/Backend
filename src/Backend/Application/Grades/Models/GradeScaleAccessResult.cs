namespace Application.Grades.Models;

public sealed class GradeScaleAccessResult
{
    private GradeScaleAccessResult(GradeScaleAccessStatus status, IReadOnlyList<GradeScaleDto> scale)
    {
        Status = status;
        Scale = scale;
    }

    public GradeScaleAccessStatus Status { get; }
    public IReadOnlyList<GradeScaleDto> Scale { get; }

    public static GradeScaleAccessResult Success(IReadOnlyList<GradeScaleDto> scale) => new(GradeScaleAccessStatus.Success, scale);
    public static GradeScaleAccessResult NotFound() => new(GradeScaleAccessStatus.NotFound, Array.Empty<GradeScaleDto>());
    public static GradeScaleAccessResult Forbidden() => new(GradeScaleAccessStatus.Forbidden, Array.Empty<GradeScaleDto>());
}
