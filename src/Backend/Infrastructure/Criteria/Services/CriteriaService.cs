using Application.Criteria.Contracts;
using Application.Criteria.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Criteria.Services;

public sealed class CriteriaService : ICriteriaService
{
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";
    private const string StudentRole = "Student";
    private const string AssignmentPostType = "Assignment";
    private const string ChecklistFormat = "checklist";
    private const string PercentageFormat = "percentage";
    private const string FivePointMode = "five_point";
    private const string CumulativeMode = "cumulative";
    private const string InstructorAssessmentType = "INSTRUCTOR";

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CriteriaService(LmsDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<CriterionListResult> GetTaskCriteriaAsync(Guid currentUserId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == taskId && x.PostType == AssignmentPostType, cancellationToken);

        if (task is null)
        {
            return CriterionListResult.NotFound();
        }

        var isParticipant = await IsParticipantAsync(currentUserId, task.SubjectId, cancellationToken);

        if (!isParticipant)
        {
            return CriterionListResult.Forbidden();
        }

        var isStudent = await IsStudentAsync(currentUserId, task.SubjectId, cancellationToken);
        var selfAssessmentEnabled = task.SelfAssessmentEnabled ?? task.Subject.SelfAssessmentEnabled;
        var hidden = isStudent
            && selfAssessmentEnabled
            && task.SelfAssessmentVisibilityDate.HasValue
            && _timeProvider.GetUtcNow() < task.SelfAssessmentVisibilityDate.Value;

        if (hidden)
        {
            return CriterionListResult.Success(Array.Empty<CriterionResponse>(), true);
        }

        var criteria = await _dbContext.Criteria
            .AsNoTracking()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id)
            .Select(x => MapCriterion(x))
            .ToListAsync(cancellationToken);

        return CriterionListResult.Success(criteria, false);
    }

    public async Task<CriterionUpdateResult> CreateAsync(Guid currentUserId, Guid taskId, CreateCriterionRequest request, CancellationToken cancellationToken)
    {
        var task = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == taskId && x.PostType == AssignmentPostType, cancellationToken);

        if (task is null)
        {
            return CriterionUpdateResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, task.SubjectId, cancellationToken))
        {
            return CriterionUpdateResult.Forbidden();
        }

        if (!IsValidCriterion(task.Subject.GradingMode, request.Format, request.Weight, request.MaxPoints))
        {
            return CriterionUpdateResult.Forbidden();
        }

        var order = request.Order ?? await GetNextOrderAsync(taskId, cancellationToken);
        var criterion = new Criterion
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Task = task,
            Description = request.Description ?? string.Empty,
            Format = request.Format!,
            Weight = string.Equals(task.Subject.GradingMode, FivePointMode, StringComparison.Ordinal) ? request.Weight : null,
            MaxPoints = string.Equals(task.Subject.GradingMode, CumulativeMode, StringComparison.Ordinal) ? request.MaxPoints : null,
            Points = request.Points,
            IsBonus = request.IsBonus,
            IsPenalty = request.IsPenalty,
            Order = order
        };

        _dbContext.Criteria.Add(criterion);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CriterionUpdateResult.Success(MapCriterion(criterion));
    }

    public async Task<CriterionUpdateResult> UpdateAsync(Guid currentUserId, Guid criterionId, UpdateCriterionRequest request, CancellationToken cancellationToken)
    {
        var criterion = await _dbContext.Criteria
            .Include(x => x.Task)
            .ThenInclude(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == criterionId, cancellationToken);

        if (criterion is null)
        {
            return CriterionUpdateResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, criterion.Task.SubjectId, cancellationToken))
        {
            return CriterionUpdateResult.Forbidden();
        }

        var nextFormat = request.Format ?? criterion.Format;
        var nextWeight = request.Weight ?? criterion.Weight;
        var nextMaxPoints = request.MaxPoints ?? criterion.MaxPoints;

        if (!IsValidCriterion(criterion.Task.Subject.GradingMode, nextFormat, nextWeight, nextMaxPoints))
        {
            return CriterionUpdateResult.Forbidden();
        }

        criterion.Description = request.Description ?? criterion.Description;
        criterion.Format = nextFormat;
        criterion.Weight = string.Equals(criterion.Task.Subject.GradingMode, FivePointMode, StringComparison.Ordinal) ? nextWeight : null;
        criterion.MaxPoints = string.Equals(criterion.Task.Subject.GradingMode, CumulativeMode, StringComparison.Ordinal) ? nextMaxPoints : null;
        criterion.Points = request.Points ?? criterion.Points;
        criterion.IsBonus = request.IsBonus ?? criterion.IsBonus;
        criterion.IsPenalty = request.IsPenalty ?? criterion.IsPenalty;
        criterion.Order = request.Order ?? criterion.Order;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CriterionUpdateResult.Success(MapCriterion(criterion));
    }

    public async Task<CriterionDeleteResult> DeleteAsync(Guid currentUserId, Guid criterionId, CancellationToken cancellationToken)
    {
        var criterion = await _dbContext.Criteria
            .Include(x => x.Task)
            .SingleOrDefaultAsync(x => x.Id == criterionId, cancellationToken);

        if (criterion is null)
        {
            return CriterionDeleteResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, criterion.Task.SubjectId, cancellationToken))
        {
            return CriterionDeleteResult.Forbidden();
        }

        _dbContext.Criteria.Remove(criterion);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CriterionDeleteResult.Success();
    }

    public async Task<CriterionResultsUpdateResult> UpsertInstructorResultsAsync(Guid currentUserId, Guid submissionId, UpsertCriterionResultsRequest request, CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.post)
            .ThenInclude(x => x.Subject)
            .Include(x => x.CriterionResults)
            .SingleOrDefaultAsync(x => x.id == submissionId, cancellationToken);

        if (submission is null)
        {
            return CriterionResultsUpdateResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, submission.post.SubjectId, cancellationToken))
        {
            return CriterionResultsUpdateResult.Forbidden();
        }

        var criteria = await _dbContext.Criteria
            .Where(x => x.TaskId == submission.assignmentId)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (request.Results.Any(x => !criteria.ContainsKey(x.CriterionId) || !IsValidResultValue(criteria[x.CriterionId].Format, x.Value)))
        {
            return CriterionResultsUpdateResult.Forbidden();
        }

        foreach (var resultRequest in request.Results)
        {
            var result = submission.CriterionResults.SingleOrDefault(x => x.CriterionId == resultRequest.CriterionId && x.AssessmentType == InstructorAssessmentType);

            if (result is null)
            {
                result = new CriterionResult
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    CriterionId = resultRequest.CriterionId,
                    CreatedBy = currentUserId.ToString(),
                    AssessmentType = InstructorAssessmentType
                };

                submission.CriterionResults.Add(result);
            }

            result.Value = resultRequest.Value;
            result.Comment = resultRequest.Comment;
            result.CreatedBy = currentUserId.ToString();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var results = submission.CriterionResults
            .Where(x => x.AssessmentType == InstructorAssessmentType)
            .OrderBy(x => x.CriterionId)
            .Select(MapCriterionResult)
            .ToList();

        return CriterionResultsUpdateResult.Success(results);
    }

    private async Task<int> GetNextOrderAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var maxOrder = await _dbContext.Criteria
            .Where(x => x.TaskId == taskId)
            .Select(x => (int?)x.Order)
            .MaxAsync(cancellationToken);

        return (maxOrder ?? 0) + 1;
    }

    private async Task<bool> IsParticipantAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId, cancellationToken);
    }

    private async Task<bool> IsStudentAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId && x.Role == StudentRole, cancellationToken);
    }

    private async Task<bool> IsTeacherOrAdminAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(
                x => x.SubjectId == subjectId
                     && x.UserId == userId
                     && (x.Role == TeacherRole || x.Role == AdminRole),
                cancellationToken);
    }

    private static bool IsValidCriterion(string gradingMode, string? format, decimal? weight, decimal? maxPoints)
    {
        if (!string.Equals(format, ChecklistFormat, StringComparison.Ordinal) && !string.Equals(format, PercentageFormat, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(gradingMode, FivePointMode, StringComparison.Ordinal))
        {
            return weight.HasValue && weight.Value > 0 && !maxPoints.HasValue;
        }

        if (string.Equals(gradingMode, CumulativeMode, StringComparison.Ordinal))
        {
            return maxPoints.HasValue && maxPoints.Value >= 0 && !weight.HasValue;
        }

        return false;
    }

    private static bool IsValidResultValue(string format, decimal value)
    {
        if (string.Equals(format, ChecklistFormat, StringComparison.Ordinal))
        {
            return value is 0 or 1;
        }

        if (string.Equals(format, PercentageFormat, StringComparison.Ordinal))
        {
            return value is 0 or 50 or 100;
        }

        return false;
    }

    private static CriterionResponse MapCriterion(Criterion criterion)
    {
        return new CriterionResponse
        {
            Id = criterion.Id,
            TaskId = criterion.TaskId,
            Description = criterion.Description,
            Format = criterion.Format,
            Weight = criterion.Weight,
            MaxPoints = criterion.MaxPoints,
            Points = criterion.Points,
            IsBonus = criterion.IsBonus,
            IsPenalty = criterion.IsPenalty,
            Order = criterion.Order
        };
    }

    private static CriterionResultResponse MapCriterionResult(CriterionResult result)
    {
        return new CriterionResultResponse
        {
            Id = result.Id,
            SubmissionId = result.SubmissionId,
            CriterionId = result.CriterionId,
            Value = result.Value,
            Comment = result.Comment,
            CreatedBy = result.CreatedBy,
            AssessmentType = result.AssessmentType
        };
    }
}
