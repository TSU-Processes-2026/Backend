using Application.Grades.Contract;
using Application.Grades.Models;
using Application.Submissions.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Grades;

public class GradesService : IGradesService
{
    private const string FivePointMode = "five_point";
    private const string CumulativeMode = "cumulative";
    private const string StudentRole = "Student";
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";
    private const string AssignmentPostType = "Assignment";
    private const string InstructorAssessmentType = "INSTRUCTOR";
    private const string ChecklistFormat = "checklist";
    private const string PercentageFormat = "percentage";
    private const string NumericFormat = "numeric";

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

        if (grade is null)
        {
            return GradesAccessResult.NotFound();
        }

        return GradesAccessResult.Success(MapGrade(grade));
    }

    public async Task<GradesAccessResult> CreateGradeAsync(Guid submissionId, int score, string verdictText, string teacherId)
    {
        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .ThenInclude(p => p.Subject)
            .ThenInclude(su => su.Participants)
            .FirstOrDefaultAsync(s => s.id == submissionId);

        if (submission is null)
        {
            return GradesAccessResult.NotFound();
        }

        if (await SubmissionBelongsToTeamAsync(submission) || submission.status != SubmissionStatusEnum.RequiresReview)
        {
            return GradesAccessResult.Forbidden();
        }

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

        return GradesAccessResult.Success(MapGrade(grade));
    }

    public async Task<GradesAccessResult> UpdateGradeAsync(Guid submissionId, int score, string verdictText, string teacherId)
    {
        var grade = await _dbContext.Grades
            .FirstOrDefaultAsync(g => g.submissionId == submissionId);

        if (grade is null)
        {
            return GradesAccessResult.NotFound();
        }

        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .ThenInclude(p => p.Subject)
            .ThenInclude(su => su.Participants)
            .FirstOrDefaultAsync(s => s.id == submissionId);

        if (submission is null)
        {
            return GradesAccessResult.NotFound();
        }

        if (await SubmissionBelongsToTeamAsync(submission))
        {
            return GradesAccessResult.Forbidden();
        }

        grade.score = score;
        grade.verdictText = verdictText;
        grade.verdictedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return GradesAccessResult.Success(MapGrade(grade));
    }

    public async Task<GradesAccessResult> DeleteGradeAsync(Guid submissionId, string teacherId)
    {
        var grade = await _dbContext.Grades
            .FirstOrDefaultAsync(g => g.submissionId == submissionId);

        if (grade is null)
        {
            return GradesAccessResult.NotFound();
        }

        var submission = await _dbContext.Submissions
            .Include(s => s.post)
            .ThenInclude(p => p.Subject)
            .ThenInclude(su => su.Participants)
            .FirstOrDefaultAsync(s => s.id == submissionId);

        if (submission is null)
        {
            return GradesAccessResult.NotFound();
        }

        if (await SubmissionBelongsToTeamAsync(submission))
        {
            return GradesAccessResult.Forbidden();
        }

        submission.status = SubmissionStatusEnum.RequiresReview;

        _dbContext.Grades.Remove(grade);
        await _dbContext.SaveChangesAsync();

        return GradesAccessResult.Success(null);
    }

    public async Task<CourseGradesListResult> GetCourseGradesAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken)
    {
        var courseExists = await _dbContext.Subjects
            .AnyAsync(x => x.Id == courseId, cancellationToken);

        if (!courseExists)
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

        var assignments = await _dbContext.Posts
            .Where(x => x.SubjectId == courseId && x.PostType == AssignmentPostType)
            .Include(x => x.Questions)
            .Include(x => x.Subject)
            .ToListAsync(cancellationToken);

        var criteria = await _dbContext.Criteria
            .Where(x => assignments.Select(a => a.Id).Contains(x.TaskId))
            .ToListAsync(cancellationToken);

        var submissions = await _dbContext.Submissions
            .Where(x => x.post.SubjectId == courseId)
            .Include(x => x.CriterionResults)
            .Include(x => x.grade)
            .ToListAsync(cancellationToken);

        foreach (var studentId in course.Participants.Where(p => p.Role == StudentRole).Select(p => p.UserId))
        {
            var finalScore = string.Equals(course.GradingMode, CumulativeMode, StringComparison.Ordinal)
                ? CalculateCumulativeScore(assignments, criteria, submissions, studentId)
                : CalculateFivePointScore(assignments, criteria, submissions, studentId);

            var finalGrade = string.Equals(course.GradingMode, CumulativeMode, StringComparison.Ordinal)
                ? ResolveCumulativeGrade(course.GradeScales.ToList(), assignments, criteria, finalScore)
                : Math.Clamp((int)Math.Round(finalScore, MidpointRounding.AwayFromZero), 2, 5).ToString();

            await UpsertCourseGradeAsync(courseId, studentId, finalScore, finalGrade, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var allGrades = await _dbContext.CourseGrades
            .AsNoTracking()
            .Where(g => g.CourseId == courseId)
            .OrderBy(g => g.StudentId)
            .ToListAsync(cancellationToken);

        return CourseGradesCalculateResult.Success(allGrades.Select(MapCourseGrade).ToList());
    }

    public async Task<GradeScaleAccessResult> GetGradeScaleAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Subjects.AnyAsync(x => x.Id == courseId, cancellationToken))
        {
            return GradeScaleAccessResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, courseId, cancellationToken))
        {
            return GradeScaleAccessResult.Forbidden();
        }

        var scale = await _dbContext.GradeScales
            .AsNoTracking()
            .Where(x => x.SubjectId == courseId)
            .OrderBy(x => x.MinPoints)
            .Select(x => MapGradeScale(x))
            .ToListAsync(cancellationToken);

        return GradeScaleAccessResult.Success(scale);
    }

    public async Task<GradeScaleAccessResult> UpsertGradeScaleAsync(Guid currentUserId, Guid courseId, UpsertGradeScaleRequest request, CancellationToken cancellationToken)
    {
        var course = await _dbContext.Subjects
            .Include(x => x.GradeScales)
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken);

        if (course is null)
        {
            return GradeScaleAccessResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, courseId, cancellationToken))
        {
            return GradeScaleAccessResult.Forbidden();
        }

        if (request.Ranges.Any(x => string.IsNullOrWhiteSpace(x.Grade) || x.MinPoints > x.MaxPoints))
        {
            return GradeScaleAccessResult.Forbidden();
        }

        _dbContext.GradeScales.RemoveRange(course.GradeScales);

        var ranges = request.Ranges
            .OrderBy(x => x.MinPoints)
            .Select(x => new GradeScale
            {
                Id = Guid.NewGuid(),
                SubjectId = courseId,
                Subject = course,
                MinPoints = x.MinPoints,
                MaxPoints = x.MaxPoints,
                Grade = x.Grade!.Trim()
            })
            .ToList();

        _dbContext.GradeScales.AddRange(ranges);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return GradeScaleAccessResult.Success(ranges.Select(MapGradeScale).ToList());
    }

    private async Task UpsertCourseGradeAsync(Guid courseId, Guid studentId, decimal finalScore, string finalGrade, CancellationToken cancellationToken)
    {
        var existingGrade = await _dbContext.CourseGrades
            .FirstOrDefaultAsync(g => g.CourseId == courseId && g.StudentId == studentId, cancellationToken);

        if (existingGrade is not null)
        {
            existingGrade.FinalScore = finalScore;
            existingGrade.FinalGrade = finalGrade;
            existingGrade.CalculatedAt = DateTimeOffset.UtcNow;
            return;
        }

        _dbContext.CourseGrades.Add(new CourseGrade
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            StudentId = studentId,
            FinalScore = finalScore,
            FinalGrade = finalGrade,
            CalculatedAt = DateTimeOffset.UtcNow
        });
    }

    private static decimal CalculateFivePointScore(IReadOnlyList<Post> assignments, IReadOnlyList<Criterion> criteria, IReadOnlyList<Submission> submissions, Guid studentId)
    {
        if (assignments.Count == 0)
        {
            return 0;
        }

        var scores = assignments
            .Select(assignment => CalculateFivePointAssignmentScore(assignment, criteria.Where(x => x.TaskId == assignment.Id).ToList(), submissions.Where(x => x.assignmentId == assignment.Id && x.authorId == studentId).ToList()))
            .ToList();

        return scores.Sum() / scores.Count;
    }

    private static decimal CalculateFivePointAssignmentScore(Post assignment, IReadOnlyList<Criterion> criteria, IReadOnlyList<Submission> submissions)
    {
        var submission = submissions
            .OrderByDescending(x => x.submittedAt)
            .FirstOrDefault();

        if (submission is null || criteria.Count == 0)
        {
            return 2;
        }

        var instructorResults = submission.CriterionResults
            .Where(x => x.AssessmentType == InstructorAssessmentType)
            .ToDictionary(x => x.CriterionId);

        var requiredCriteria = criteria.Where(x => !x.IsBonus && !x.IsPenalty).ToList();
        var maxWeight = requiredCriteria.Sum(x => x.Weight ?? 0);

        if (maxWeight <= 0)
        {
            return 2;
        }

        var achievedWeight = requiredCriteria.Sum(x => (x.Weight ?? 0) * GetCompletionRatio(x, instructorResults));
        var percent = achievedWeight / maxWeight * 100;
        var score = percent >= 90 ? 5 : percent >= 75 ? 4 : percent >= 60 ? 3 : 2;

        if (criteria.Any(x => x.IsBonus && GetCompletionRatio(x, instructorResults) > 0))
        {
            score++;
        }

        if (criteria.Any(x => x.IsPenalty && GetCompletionRatio(x, instructorResults) > 0))
        {
            score--;
        }

        return Math.Clamp(score, 2, 5);
    }

    private static decimal CalculateCumulativeScore(IReadOnlyList<Post> assignments, IReadOnlyList<Criterion> criteria, IReadOnlyList<Submission> submissions, Guid studentId)
    {
        return assignments.Sum(assignment => CalculateCumulativeAssignmentScore(assignment, criteria.Where(x => x.TaskId == assignment.Id).ToList(), submissions.Where(x => x.assignmentId == assignment.Id && x.authorId == studentId).ToList()));
    }

    private static decimal CalculateCumulativeAssignmentScore(Post assignment, IReadOnlyList<Criterion> criteria, IReadOnlyList<Submission> submissions)
    {
        var submission = submissions
            .OrderByDescending(x => x.submittedAt)
            .FirstOrDefault();

        if (submission is null)
        {
            return 0;
        }

        var instructorResults = submission.CriterionResults
            .Where(x => x.AssessmentType == InstructorAssessmentType)
            .ToDictionary(x => x.CriterionId);

        var total = criteria.Sum(criterion =>
        {
            var points = (criterion.MaxPoints ?? 0) * GetCompletionRatio(criterion, instructorResults);
            return criterion.IsPenalty ? -points : points;
        });

        return Math.Max(0, total);
    }

    private static decimal GetCompletionRatio(Criterion criterion, IReadOnlyDictionary<Guid, CriterionResult> results)
    {
        if (!results.TryGetValue(criterion.Id, out var result) || !result.Value.HasValue)
        {
            return 0;
        }

        if (string.Equals(criterion.Format, ChecklistFormat, StringComparison.Ordinal))
        {
            return result.Value.Value == 1 ? 1 : 0;
        }

        if (string.Equals(criterion.Format, PercentageFormat, StringComparison.Ordinal))
        {
            return Math.Clamp(result.Value.Value, 0, 100) / 100;
        }

        if (string.Equals(criterion.Format, NumericFormat, StringComparison.Ordinal))
        {
            var max = criterion.MaxPoints ?? 1;
            return max > 0 ? Math.Clamp(result.Value.Value, 0, max) / max : 0;
        }

        return 0;
    }

    private static string ResolveCumulativeGrade(IReadOnlyList<GradeScale> gradeScales, IReadOnlyList<Post> assignments, IReadOnlyList<Criterion> criteria, decimal finalScore)
    {
        var configuredGrade = gradeScales
            .OrderByDescending(x => x.MinPoints)
            .FirstOrDefault(x => finalScore >= x.MinPoints && finalScore <= x.MaxPoints);

        if (configuredGrade is not null)
        {
            return configuredGrade.Grade;
        }

        var maxScore = criteria
            .Where(x => !x.IsBonus && !x.IsPenalty)
            .Sum(x => x.MaxPoints ?? 0);

        if (maxScore <= 0)
        {
            maxScore = assignments.Sum(x => x.MaxPoints ?? 0);
        }

        if (maxScore <= 0)
        {
            return "2";
        }

        var percent = finalScore / maxScore * 100;

        return percent >= 80 ? "5" : percent >= 60 ? "4" : percent >= 40 ? "3" : "2";
    }

    private async Task<bool> SubmissionBelongsToTeamAsync(Submission submission)
    {
        return await _dbContext.TeamGrades
            .AnyAsync(x => x.SubmissionId == submission.id);
    }

    private async Task<bool> IsTeacherOrAdminAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId && (x.Role == TeacherRole || x.Role == AdminRole), cancellationToken);
    }

    private static GradeDto MapGrade(Grade grade)
    {
        return new GradeDto
        {
            id = grade.id,
            submissionId = grade.submissionId,
            score = grade.score,
            verdictText = grade.verdictText,
            verdictedAt = grade.verdictedAt
        };
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

    private static GradeScaleDto MapGradeScale(GradeScale scale)
    {
        return new GradeScaleDto
        {
            Id = scale.Id,
            CourseId = scale.SubjectId,
            MinPoints = scale.MinPoints,
            MaxPoints = scale.MaxPoints,
            Grade = scale.Grade
        };
    }
}
