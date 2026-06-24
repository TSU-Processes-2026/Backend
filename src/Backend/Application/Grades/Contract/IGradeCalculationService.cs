using Application.Grades.Models;

namespace Application.Grades.Contract;

public interface IGradeCalculationService
{
    Task<FinalGradeResult?> CalculateSubmissionGradeAsync(Guid submissionId, CancellationToken ct);
    Task<IReadOnlyList<FinalGradeResult>> CalculateCourseGradesAsync(Guid courseId, CancellationToken ct);
}
