namespace Application.Grades.Models;

public sealed class CourseGradesCalculateResult
{
    private CourseGradesCalculateResult(CourseGradesCalculateStatus status, IReadOnlyList<CourseGradeDto>? grades = null)
    {
        Status = status;
        Grades = grades;
    }

    public CourseGradesCalculateStatus Status { get; }
    public IReadOnlyList<CourseGradeDto>? Grades { get; }

    public static CourseGradesCalculateResult Success(IReadOnlyList<CourseGradeDto> grades) => new(CourseGradesCalculateStatus.Success, grades);
    public static CourseGradesCalculateResult NotFound() => new(CourseGradesCalculateStatus.NotFound);
    public static CourseGradesCalculateResult Forbidden() => new(CourseGradesCalculateStatus.Forbidden);
}

public enum CourseGradesCalculateStatus
{
    Success,
    NotFound,
    Forbidden
}
