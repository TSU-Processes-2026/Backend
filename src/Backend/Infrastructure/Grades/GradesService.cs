using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Grades
{
    public class GradesService : IGradesService
    {
        private readonly LmsDbContext _dbContext;

        public GradesService(LmsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GradesAccessResult> GetGradeAsync(Guid submissionId)
        {
            var grade = await _dbContext.Grades
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.submissionId == submissionId);

            if (grade == null)
                return GradesAccessResult.NotFound();

            return GradesAccessResult.Success(new GradeDto
            {
                id = grade.id,
                submissionId = grade.submissionId,
                score = grade.score,
                verdictText = grade.verdictText,
                verdictedAt = grade.verdictedAt
            });
        }

        public async Task<GradesAccessResult> CreateGradeAsync(Guid submissionId, int score, string verdictText, string teacherId)
        {
            var submission = await _dbContext.Submissions
                .FirstOrDefaultAsync(s => s.id == submissionId);

            if (submission == null)
                return GradesAccessResult.NotFound();

            if (submission.status != SubmissionStatusEnum.RequiresReview)
                return GradesAccessResult.Forbidden();

            var grade = new Grade
            {
                id = Guid.NewGuid(),
                submissionId = submissionId,
                score = score,
                verdictText = verdictText,
                verdictedAt = DateTime.UtcNow
            };

            submission.status = SubmissionStatusEnum.Graded;

            _dbContext.Grades.Add(grade);
            await _dbContext.SaveChangesAsync();

            return GradesAccessResult.Success(new GradeDto
            {
                id = grade.id,
                submissionId = grade.submissionId,
                score = grade.score,
                verdictText = grade.verdictText,
                verdictedAt = grade.verdictedAt
            });
        }
    }
}