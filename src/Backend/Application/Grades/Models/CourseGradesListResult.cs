namespace Application.Grades.Models;

public sealed class CourseGradesListResult
{
    private CourseGradesListResult(CourseGradesListStatus status, IReadOnlyList<CourseGradeDto>? grades = null)
    {
        Status = status;
        Grades = grades;
    }

    public CourseGradesListStatus Status { get; }
    public IReadOnlyList<CourseGradeDto>? Grades { get; }

    public static CourseGradesListResult Success(IReadOnlyList<CourseGradeDto> grades) => new(CourseGradesListStatus.Success, grades);
    public static CourseGradesListResult NotFound() => new(CourseGradesListStatus.NotFound);
    public static CourseGradesListResult Forbidden() => new(CourseGradesListStatus.Forbidden);
}

public enum CourseGradesListStatus
{
    Success,
    NotFound,
    Forbidden
}
