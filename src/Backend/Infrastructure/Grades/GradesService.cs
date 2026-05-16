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

        public async Task<CourseGradesListResult> GetCourseGradesAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken)
        {
            var course = await _dbContext.Subjects
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == courseId, cancellationToken);

            if (course is null)
            {
                return CourseGradesListResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, courseId, cancellationToken))
            {
                return CourseGradesListResult.Forbidden();
            }

            var grades = await _dbContext.CourseGrades
                .AsNoTracking()
                .Where(g => g.CourseId == courseId)
                .OrderBy(g => g.StudentId)
                .ToListAsync(cancellationToken);

            return CourseGradesListResult.Success(grades.Select(MapCourseGrade).ToList());
        }

        public async Task<CourseGradesCalculateResult> CalculateCourseGradesAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken)
        {
            var course = await _dbContext.Subjects
                .Include(s => s.Participants)
                .Include(s => s.GradeScales)
                .FirstOrDefaultAsync(s => s.Id == courseId, cancellationToken);

            if (course is null)
            {
                return CourseGradesCalculateResult.NotFound();
            }

            if (!await IsTeacherOrAdminAsync(currentUserId, courseId, cancellationToken))
            {
                return CourseGradesCalculateResult.Forbidden();
            }

            // Get all students in the course
            var studentIds = course.Participants
                .Where(p => p.Role == "Student")
                .Select(p => p.UserId)
                .ToList();

            var calculatedGrades = new List<CourseGrade>();

            foreach (var studentId in studentIds)
            {
                // Get all grades for this student in this course
                var studentGrades = await _dbContext.Grades
                    .Include(g => g.submission)
                        .ThenInclude(s => s.post)
                    .Where(g => g.submission.post.SubjectId == courseId && g.submission.authorId == studentId)
                    .ToListAsync(cancellationToken);

                if (studentGrades.Any())
                {
                    decimal finalScore;
                    string finalGrade;

                    if (course.GradingMode == "five_point")
                    {
                        // Five-point mode: average of assignment grades, rounded to 2-5
                        var averageScore = (decimal)studentGrades.Average(g => g.score);
                        finalScore = averageScore;
                        finalGrade = averageScore >= 80 ? "5" : averageScore >= 60 ? "4" : averageScore >= 40 ? "3" : "2";
                    }
                    else if (course.GradingMode == "cumulative")
                    {
                        // Cumulative mode: sum of points with grade scale
                        // For now, use the sum of scores as the total points
                        var totalPoints = studentGrades.Sum(g => g.score);
                        finalScore = totalPoints;

                        // Find the grade from the grade scale
                        var gradeScale = course.GradeScales
                            .FirstOrDefault(gs => totalPoints >= gs.MinScore && totalPoints <= gs.MaxScore);

                        finalGrade = gradeScale?.Grade ?? "N/A";
                    }
                    else
                    {
                        // Default to five-point mode if unknown mode
                        var averageScore = (decimal)studentGrades.Average(g => g.score);
                        finalScore = averageScore;
                        finalGrade = averageScore >= 80 ? "5" : averageScore >= 60 ? "4" : averageScore >= 40 ? "3" : "2";
                    }

                    var existingGrade = await _dbContext.CourseGrades
                        .FirstOrDefaultAsync(g => g.CourseId == courseId && g.StudentId == studentId, cancellationToken);

                    if (existingGrade is not null)
                    {
                        existingGrade.FinalScore = finalScore;
                        existingGrade.FinalGrade = finalGrade;
                        existingGrade.CalculatedAt = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        calculatedGrades.Add(new CourseGrade
                        {
                            Id = Guid.NewGuid(),
                            CourseId = courseId,
                            StudentId = studentId,
                            FinalScore = finalScore,
                            FinalGrade = finalGrade,
                            CalculatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
            }

            if (calculatedGrades.Any())
            {
                _dbContext.CourseGrades.AddRange(calculatedGrades);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var allGrades = await _dbContext.CourseGrades
                .AsNoTracking()
                .Where(g => g.CourseId == courseId)
                .OrderBy(g => g.StudentId)
                .ToListAsync(cancellationToken);

            return CourseGradesCalculateResult.Success(allGrades.Select(MapCourseGrade).ToList());
        }

        private async Task<bool> IsTeacherOrAdminAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
        {
            return await _dbContext.SubjectParticipants
                .AnyAsync(
                    x => x.SubjectId == subjectId
                         && x.UserId == userId
                         && (x.Role == "Teacher" || x.Role == "Admin"),
                    cancellationToken);
        }

        private static CourseGradeDto MapCourseGrade(CourseGrade grade)
        {
            return new CourseGradeDto
            {
                Id = grade.Id,
                CourseId = grade.CourseId,
                StudentId = grade.StudentId,
                FinalScore = grade.FinalScore,
                FinalGrade = grade.FinalGrade,
                CalculatedAt = grade.CalculatedAt
            };
        }
    }
}
