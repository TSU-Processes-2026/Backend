using Application.Criteria.Models;
using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Submissions.Services;

public class SubmissionsService : ISubmissionsService
{
    private const string StudentRole = "Student";
    private const string SelfAssessmentType = "SELF";

    private readonly LmsDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubmissionsService(LmsDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<SubmissionAccessResult> CreateSubmission(Guid assignmentId, Guid authorId, SubmissionCreateRequest request)
    {
        var user = await _userManager.FindByIdAsync(authorId.ToString());

        if (user is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        var post = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == assignmentId);

        if (post is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        if (!await IsStudentAsync(authorId, post.SubjectId))
        {
            return SubmissionAccessResult.Forbidden();
        }

        if (post.DeadLine.HasValue && DateTimeOffset.UtcNow > post.DeadLine.Value)
        {
            return SubmissionAccessResult.Forbidden();
        }

        var existingSubmission = await _dbContext.Submissions
            .Include(x => x.answers)
            .Include(x => x.DecisionSession)
            .Include(x => x.TeamGrade)
            .Include(x => x.CriterionResults)
            .FirstOrDefaultAsync(x => x.assignmentId == assignmentId && x.authorId == authorId && x.status != SubmissionStatusEnum.Graded);

        var answers = request.answers.Select(a => new AnswerItem
        {
            id = Guid.NewGuid(),
            assignmentQuestionId = a.assignmentQuestionId,
            answerType = a.answerType,
            selectedOptionId = a.selectedOptionId,
            selectedOptionsId = a.selectedOptionIds,
            text = a.text
        }).ToList();

        if (existingSubmission is not null)
        {
            if (existingSubmission.status != SubmissionStatusEnum.Draft)
            {
                return SubmissionAccessResult.Success(MapToDto(existingSubmission));
            }

            existingSubmission.answers ??= new List<AnswerItem>();
            existingSubmission.answers.Clear();

            foreach (var answer in answers)
            {
                existingSubmission.answers.Add(answer);
            }

            existingSubmission.submittedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return SubmissionAccessResult.Success(MapToDto(existingSubmission));
        }

        var submission = new Submission
        {
            id = Guid.NewGuid(),
            assignmentId = assignmentId,
            authorId = authorId,
            post = post,
            answers = answers,
            status = SubmissionStatusEnum.Draft,
            submittedAt = DateTime.UtcNow
        };

        _dbContext.Submissions.Add(submission);
        await _dbContext.SaveChangesAsync();

        return SubmissionAccessResult.Success(MapToDto(submission));
    }

    public async Task<SubmissionAccessResult> CreateSubmissionWithSelfAssessment(Guid taskId, Guid authorId, SubmissionWithSelfAssessmentRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(authorId.ToString());

        if (user is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        var task = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == taskId, cancellationToken);

        if (task is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        if (!await IsStudentAsync(authorId, task.SubjectId, cancellationToken))
        {
            return SubmissionAccessResult.Forbidden();
        }

        if (task.DeadLine.HasValue && DateTimeOffset.UtcNow > task.DeadLine.Value)
        {
            return SubmissionAccessResult.Forbidden();
        }

        var criteria = await _dbContext.Criteria
            .Where(c => c.TaskId == taskId)
            .ToListAsync(cancellationToken);

        var selfAssessmentRequired = task.SelfAssessmentEnabled ?? task.Subject.SelfAssessmentEnabled;

        if (selfAssessmentRequired && !SelfAssessmentsAreValid(task, criteria, request.SelfAssessments))
        {
            return SubmissionAccessResult.Forbidden();
        }

        var existingSubmission = await _dbContext.Submissions
            .Include(x => x.CriterionResults)
            .FirstOrDefaultAsync(x => x.assignmentId == taskId && x.authorId == authorId && x.status != SubmissionStatusEnum.Graded, cancellationToken);

        if (existingSubmission is not null)
        {
            if (existingSubmission.status != SubmissionStatusEnum.Draft)
            {
                return SubmissionAccessResult.Success(MapToDto(existingSubmission));
            }

            ReplaceSelfAssessments(existingSubmission, request.SelfAssessments, authorId);
            ReplaceAnswers(existingSubmission, request.Answers);
            existingSubmission.submittedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return SubmissionAccessResult.Success(MapToDto(existingSubmission));
        }

        var submission = new Submission
        {
            id = Guid.NewGuid(),
            assignmentId = taskId,
            authorId = authorId,
            post = task,
            status = SubmissionStatusEnum.Draft,
            submittedAt = DateTime.UtcNow
        };

        ReplaceSelfAssessments(submission, request.SelfAssessments, authorId);
        ReplaceAnswers(submission, request.Answers);

        _dbContext.Submissions.Add(submission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubmissionAccessResult.Success(MapToDto(submission));
    }

    public async Task<List<SubmissionDto>> GetSubmissions(Guid assignmentId, int limit, int offset)
    {
        var submissions = await _dbContext.Submissions
            .Where(x => x.assignmentId == assignmentId)
            .Include(x => x.answers)
            .Include(x => x.DecisionSession)
            .Include(x => x.TeamGrade)
            .Include(x => x.CriterionResults)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return submissions.Select(MapToDto).ToList();
    }

    public async Task<List<SubmissionDto>> GetUserSubmissions(Guid assignmentId, Guid authorId, int limit, int offset)
    {
        var submissions = await _dbContext.Submissions
            .Where(x => x.assignmentId == assignmentId && x.authorId == authorId)
            .Include(x => x.answers)
            .Include(x => x.DecisionSession)
            .Include(x => x.TeamGrade)
            .Include(x => x.CriterionResults)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return submissions.Select(MapToDto).ToList();
    }

    public async Task<SubmissionAccessResult> GetSubmission(Guid submissionId)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.answers)
            .Include(x => x.DecisionSession)
            .Include(x => x.TeamGrade)
            .Include(x => x.CriterionResults)
            .FirstOrDefaultAsync(x => x.id == submissionId);

        if (submission is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        return SubmissionAccessResult.Success(MapToDto(submission));
    }

    public async Task<SubmissionAccessResult> PatchSubmission(Guid submissionId, SubmissionCreateRequest request)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.answers)
            .Include(x => x.CriterionResults)
            .FirstOrDefaultAsync(x => x.id == submissionId);

        if (submission is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        if (submission.status != SubmissionStatusEnum.Draft)
        {
            return SubmissionAccessResult.Forbidden();
        }

        submission.answers?.Clear();

        foreach (var answer in request.answers)
        {
            submission.answers ??= new List<AnswerItem>();
            submission.answers.Add(new AnswerItem
            {
                id = Guid.NewGuid(),
                assignmentQuestionId = answer.assignmentQuestionId,
                answerType = answer.answerType,
                selectedOptionId = answer.selectedOptionId,
                selectedOptionsId = answer.selectedOptionIds,
                text = answer.text
            });
        }

        await _dbContext.SaveChangesAsync();

        return SubmissionAccessResult.Success(MapToDto(submission));
    }

    public async Task<SubmissionAccessResult> SubmitSubmission(Guid submissionId)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.post)
            .ThenInclude(x => x.Subject)
            .Include(x => x.CriterionResults)
            .FirstOrDefaultAsync(x => x.id == submissionId);

        if (submission is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        if (submission.status != SubmissionStatusEnum.Draft)
        {
            return SubmissionAccessResult.Forbidden();
        }

        if (submission.post.DeadLine.HasValue && DateTimeOffset.UtcNow > submission.post.DeadLine.Value)
        {
            return SubmissionAccessResult.Forbidden();
        }

        var criteria = await _dbContext.Criteria
            .Where(x => x.TaskId == submission.assignmentId)
            .ToListAsync();

        var selfAssessmentRequired = submission.post.SelfAssessmentEnabled ?? submission.post.Subject.SelfAssessmentEnabled;

        if (selfAssessmentRequired && !StoredSelfAssessmentsAreComplete(submission, criteria))
        {
            return SubmissionAccessResult.Forbidden();
        }

        submission.status = SubmissionStatusEnum.RequiresReview;

        await _dbContext.SaveChangesAsync();

        return SubmissionAccessResult.Success(MapToDto(submission));
    }

    public async Task<SubmissionAccessResult> WithdrawSubmission(Guid submissionId)
    {
        var submission = await _dbContext.Submissions
            .Include(x => x.CriterionResults)
            .FirstOrDefaultAsync(x => x.id == submissionId);

        if (submission is null)
        {
            return SubmissionAccessResult.NotFound();
        }

        if (submission.status != SubmissionStatusEnum.RequiresReview)
        {
            return SubmissionAccessResult.Forbidden();
        }

        submission.status = SubmissionStatusEnum.Draft;

        await _dbContext.SaveChangesAsync();

        return SubmissionAccessResult.Success(MapToDto(submission));
    }

    private async Task<bool> IsStudentAsync(Guid userId, Guid subjectId)
    {
        return await IsStudentAsync(userId, subjectId, CancellationToken.None);
    }

    private async Task<bool> IsStudentAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId && x.Role == StudentRole, cancellationToken);
    }

    private static bool SelfAssessmentsAreValid(Post task, IReadOnlyList<Criterion> criteria, IReadOnlyList<SelfAssessmentRequest>? selfAssessments)
    {
        if (task.SelfAssessmentVisibilityDate.HasValue && DateTimeOffset.UtcNow < task.SelfAssessmentVisibilityDate.Value)
        {
            return false;
        }

        if (selfAssessments is null || selfAssessments.Count == 0)
        {
            return criteria.Count == 0;
        }

        var requestByCriterionId = selfAssessments.ToDictionary(x => x.CriterionId);

        return criteria.All(criterion => requestByCriterionId.TryGetValue(criterion.Id, out var result) && IsValidResultValue(criterion.Format, result.Value));
    }

    private static bool StoredSelfAssessmentsAreComplete(Submission submission, IReadOnlyList<Criterion> criteria)
    {
        if (submission.post.SelfAssessmentVisibilityDate.HasValue && DateTimeOffset.UtcNow < submission.post.SelfAssessmentVisibilityDate.Value)
        {
            return false;
        }

        var selfCriterionIds = submission.CriterionResults
            .Where(x => x.AssessmentType == SelfAssessmentType)
            .Select(x => x.CriterionId)
            .ToHashSet();

        return criteria.All(x => selfCriterionIds.Contains(x.Id));
    }

    private void ReplaceSelfAssessments(Submission submission, IReadOnlyList<SelfAssessmentRequest>? selfAssessments, Guid authorId)
    {
        _dbContext.CriterionResults.RemoveRange(submission.CriterionResults.Where(x => x.AssessmentType == SelfAssessmentType));

        if (selfAssessments is null)
        {
            return;
        }

        foreach (var selfAssessment in selfAssessments)
        {
            submission.CriterionResults.Add(new CriterionResult
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.id,
                CriterionId = selfAssessment.CriterionId,
                Value = selfAssessment.Value,
                Comment = selfAssessment.Comment,
                CreatedBy = authorId.ToString(),
                AssessmentType = SelfAssessmentType
            });
        }
    }

    private static void ReplaceAnswers(Submission submission, IReadOnlyList<AnswerItemDto>? answers)
    {
        if (answers is null)
        {
            return;
        }

        submission.answers ??= new List<AnswerItem>();
        submission.answers.Clear();

        foreach (var answer in answers)
        {
            submission.answers.Add(new AnswerItem
            {
                id = Guid.NewGuid(),
                assignmentQuestionId = answer.assignmentQuestionId,
                answerType = answer.answerType,
                selectedOptionId = answer.selectedOptionId,
                selectedOptionsId = answer.selectedOptionIds,
                text = answer.text
            });
        }
    }

    private static bool IsValidResultValue(string format, decimal value)
    {
        if (string.Equals(format, "checklist", StringComparison.Ordinal))
        {
            return value is 0 or 1;
        }

        if (string.Equals(format, "percentage", StringComparison.Ordinal))
        {
            return value is 0 or 50 or 100;
        }

        return false;
    }

    private static SubmissionDto MapToDto(Submission submission)
    {
        return new SubmissionDto
        {
            id = submission.id,
            assignmentId = submission.assignmentId,
            authorId = submission.authorId,
            status = submission.status,
            submittedAt = submission.submittedAt,
            decisionResult = submission.DecisionSession?.Result,
            hasDecisionSession = submission.DecisionSession is not null,
            isDecisionSessionClosed = submission.DecisionSession?.IsClosed ?? false,
            isFinalTeamDecision = submission.DecisionSession?.IsClosed == true && submission.DecisionSession.Result == DecisionResult.Approved,
            answers = submission.answers?.Select(a => new AnswerItemDto
            {
                id = a.id,
                assignmentQuestionId = a.assignmentQuestionId,
                answerType = a.answerType,
                selectedOptionId = a.selectedOptionId,
                selectedOptionIds = a.selectedOptionsId,
                text = a.text
            }).ToList(),
            criterionResults = submission.CriterionResults
                .OrderBy(x => x.CriterionId)
                .ThenBy(x => x.AssessmentType)
                .Select(MapCriterionResult)
                .ToList()
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
