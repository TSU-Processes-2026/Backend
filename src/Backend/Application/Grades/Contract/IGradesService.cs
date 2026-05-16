using Application.Grades.Models;
using Application.Submissions.Models;
using System;
using System.Threading.Tasks;

namespace Application.Grades.Contract
{
    public interface IGradesService
    {
        Task<GradesAccessResult> GetGradeAsync(Guid submissionId);
        Task<GradesAccessResult> CreateGradeAsync(Guid submissionId, int score, string verdictText, string teacherId);
        Task<GradesAccessResult> UpdateGradeAsync(Guid submissionId, int score, string verdictText, string teacherId);
        Task<GradesAccessResult> DeleteGradeAsync(Guid submissionId, string teacherId);
        Task<CourseGradesListResult> GetCourseGradesAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken);
        Task<CourseGradesCalculateResult> CalculateCourseGradesAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken);
    }
}
