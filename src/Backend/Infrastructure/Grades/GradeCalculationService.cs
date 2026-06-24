using Application.Grades.Contract;
using Application.Grades.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Grades;

public sealed class GradeCalculationService : IGradeCalculationService
{
    private readonly LmsDbContext _dbContext;

    public GradeCalculationService(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinalGradeResult?> CalculateSubmissionGradeAsync(Guid submissionId, CancellationToken ct)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.post)
            .SingleOrDefaultAsync(x => x.id == submissionId, ct);

        if (submission is null) return null;

        var reviews = await _dbContext.Reviews
            .Include(x => x.Assignment)
            .Where(x => x.Assignment.SubmissionId == submissionId
                && x.IsFinal
                && !x.IsRejected)
            .ToListAsync(ct);

        decimal? score = null;
        string? source = null;

        if (submission.FinalSource == "teacher" && submission.FinalScore.HasValue)
        {
            score = submission.FinalScore;
            source = "teacher";
        }
        else
        {
            var teacherReview = reviews.FirstOrDefault(x => x.Source == "teacher");

            if (teacherReview is not null)
            {
                score = teacherReview.OverallScore;
                source = "teacher";
            }
            else
            {
                var peerReviews = reviews.Where(x => x.Source == "peer").ToList();
                var scoredPeerReviews = peerReviews.Where(x => x.OverallScore.HasValue).ToList();

                if (scoredPeerReviews.Count > 0)
                {
                    score = scoredPeerReviews.Average(x => x.OverallScore!.Value);
                    source = "peer";
                }
            }
        }

        // Apply passive criterion penalties and bonuses
        if (score.HasValue)
        {
            var criteria = await _dbContext.Criteria
                .Where(x => x.TaskId == submission.assignmentId && x.CriterionType == "passive")
                .ToListAsync(ct);

            if (criteria.Count > 0)
            {
                var criterionResults = await _dbContext.CriterionResults
                    .Where(x => x.SubmissionId == submissionId && criteria.Select(c => c.Id).Contains(x.CriterionId))
                    .ToListAsync(ct);

                var resultsByCriterionId = criterionResults
                    .GroupBy(x => x.CriterionId)
                    .ToDictionary(g => g.Key, g => g.Average(x => x.Value ?? 0));

                foreach (var criterion in criteria)
                {
                    if (!resultsByCriterionId.TryGetValue(criterion.Id, out var avgValue))
                        continue;

                    if (criterion.IsPenalty && avgValue > 0)
                    {
                        score -= avgValue;
                    }
                    else if (criterion.IsBonus && avgValue > 0)
                    {
                        score += avgValue;
                    }
                }

                // Clamp to valid range [0, submission.post.MaxPoints ?? 100]
                var maxPoints = submission.post.MaxPoints ?? 100;
                if (score < 0) score = 0;
                if (score > maxPoints) score = maxPoints;
            }
        }

        var finalGrade = await _dbContext.FinalGrades
            .SingleOrDefaultAsync(x => x.SubmissionId == submissionId, ct);

        if (finalGrade is null)
        {
            finalGrade = new FinalGrade
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                StudentId = submission.authorId,
                FinalScore = score,
                FinalSource = source,
                CalculatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.FinalGrades.Add(finalGrade);
        }
        else
        {
            finalGrade.FinalScore = score;
            finalGrade.FinalSource = source;
            finalGrade.CalculatedAt = DateTimeOffset.UtcNow;
        }

        submission.FinalScore = score;
        submission.FinalSource = source;

        await _dbContext.SaveChangesAsync(ct);
        return MapToResult(finalGrade);
    }

    public async Task<IReadOnlyList<FinalGradeResult>> CalculateCourseGradesAsync(Guid courseId, CancellationToken ct)
    {
        var submissions = await _dbContext.Submissions
            .Where(x => x.post.SubjectId == courseId)
            .Select(x => x.id)
            .ToListAsync(ct);

        var results = new List<FinalGradeResult>();
        foreach (var submissionId in submissions)
        {
            var result = await CalculateSubmissionGradeAsync(submissionId, ct);
            if (result is not null)
                results.Add(result);
        }

        return results;
    }

    private static FinalGradeResult MapToResult(FinalGrade grade)
    {
        return new FinalGradeResult
        {
            Id = grade.Id,
            SubmissionId = grade.SubmissionId,
            FinalScore = grade.FinalScore,
            FinalSource = grade.FinalSource,
            CalculatedAt = grade.CalculatedAt
        };
    }
}
