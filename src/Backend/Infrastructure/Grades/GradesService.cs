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
                .Include(s => s.post)
                    .ThenInclude(p => p.Subject)
                        .ThenInclude(su => su.Participants)
                .FirstOrDefaultAsync(s => s.id == submissionId);



            if (submission == null)
                return GradesAccessResult.NotFound();

            if (await SubmissionBelongsToTeamAsync(submission))
            {
                Console.WriteLine("Team error");
                return GradesAccessResult.Forbidden();
            }
                

            if (submission.status != SubmissionStatusEnum.RequiresReview)
            {
                Console.WriteLine("Submission status error");
                return GradesAccessResult.Forbidden();
            }

            //SubjectParticipant? participant = submission?.post?.Subject?.Participants?.Where(p => p.UserId.ToString() == teacherId && p.Role == "Teacher").FirstOrDefault();//.Where(p => p.id.ToString() == teacherId && p.)

            //if (participant is null)
            //{
            //    Console.WriteLine("Participant error");
            //    return GradesAccessResult.Forbidden();
            //}

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

        public async Task<GradesAccessResult> UpdateGradeAsync(Guid submissionId, int score, string verdictText, string teacherId)
        {
            var grade = await _dbContext.Grades
                .FirstOrDefaultAsync(g => g.submissionId == submissionId);

            if (grade == null)
            {
                Console.WriteLine("Grade is null");
                return GradesAccessResult.NotFound();
            }
                

            var submission = await _dbContext.Submissions
                .Include(s => s.post)
                    .ThenInclude(p => p.Subject)
                        .ThenInclude(su => su.Participants)
                .FirstOrDefaultAsync(s => s.id == submissionId);

            if (submission == null)
                return GradesAccessResult.NotFound();

            if (await SubmissionBelongsToTeamAsync(submission))
                return GradesAccessResult.Forbidden();

            grade.score = score;
            grade.verdictText = verdictText;
            grade.verdictedAt = DateTime.UtcNow;

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

        public async Task<GradesAccessResult> DeleteGradeAsync(Guid submissionId, string teacherId)
        {
            var grade = await _dbContext.Grades
                .FirstOrDefaultAsync(g => g.submissionId == submissionId);

            if (grade == null)
                return GradesAccessResult.NotFound();

            var submission = await _dbContext.Submissions
                .Include(s => s.post)
                    .ThenInclude(p => p.Subject)
                        .ThenInclude(su => su.Participants)
                .FirstOrDefaultAsync(s => s.id == submissionId);

            if (submission == null)
                return GradesAccessResult.NotFound();

            if (await SubmissionBelongsToTeamAsync(submission))
            {
                Console.WriteLine("Team error");
                return GradesAccessResult.Forbidden();
            }


            submission.status = SubmissionStatusEnum.RequiresReview;

            _dbContext.Grades.Remove(grade);
            await _dbContext.SaveChangesAsync();

            return GradesAccessResult.Success(null);
        }

        private async Task<bool> SubmissionBelongsToTeamAsync(Submission submission)
        {
            return await _dbContext.TeamGrades
                .AnyAsync(x => x.SubmissionId == submission.id);
        }
    }
}
