using Application.Submissions.Contracts;
using Application.Submissions.Models;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Submissions.Services
{
    public class SubmissionsService : ISubmissionsService
    {
        private const string AdminRole = "Admin";
        private const string TeacherRole = "Teacher";
        private const string StudentRole = "Student";

        private readonly LmsDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubmissionsService(
            LmsDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<SubmissionAccessResult> CreateSubmission(
    Guid assignmentId,
    Guid authorId,
    SubmissionCreateRequest request)
        {
            var user = await _userManager.FindByIdAsync(authorId.ToString());
            if (user == null)
                return SubmissionAccessResult.NotFound();

            var post = await _dbContext.Posts
                .Include(x => x.Subject)
                .SingleOrDefaultAsync(x => x.Id == assignmentId);

            if (post == null)
                return SubmissionAccessResult.NotFound();

            var isStudent = await _dbContext.SubjectParticipants
                .AnyAsync(x =>
                    x.SubjectId == post.SubjectId &&
                    x.UserId == authorId &&
                    x.Role == StudentRole);

            if (!isStudent)
                return SubmissionAccessResult.Forbidden();

            var existingSubmission = await _dbContext.Submissions
                .Include(x => x.answers)
                .Include(x => x.DecisionSession)
                .Include(x => x.TeamGrade)
                .FirstOrDefaultAsync(x =>
                    x.assignmentId == assignmentId &&
                    x.authorId == authorId &&
                    x.status != SubmissionStatusEnum.Graded);

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

        public async Task<SubmissionAccessResult> CreateSubmissionWithSelfAssessment(
            Guid taskId,
            Guid authorId,
            SubmissionWithSelfAssessmentRequest request,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(authorId.ToString());
            if (user == null)
                return SubmissionAccessResult.NotFound();

            var task = await _dbContext.Posts
                .Include(x => x.Subject)
                .SingleOrDefaultAsync(x => x.Id == taskId, cancellationToken);

            if (task == null)
                return SubmissionAccessResult.NotFound();

            var isStudent = await _dbContext.SubjectParticipants
                .AnyAsync(x =>
                    x.SubjectId == task.SubjectId &&
                    x.UserId == authorId &&
                    x.Role == StudentRole, cancellationToken);

            if (!isStudent)
                return SubmissionAccessResult.Forbidden();

            // Check self-assessment requirements
            if (task.SelfAssessmentEnabled)
            {
                var now = DateTimeOffset.UtcNow;
                var visibilityDate = task.SelfAssessmentVisibilityDate;

                // If visibility date is set and not yet reached, criteria are hidden
                if (visibilityDate.HasValue && now < visibilityDate.Value)
                {
                    return SubmissionAccessResult.Forbidden();
                }

                // Self-assessments are required when enabled
                if (request.SelfAssessments == null || !request.SelfAssessments.Any())
                {
                    return SubmissionAccessResult.Forbidden();
                }

                // Validate that all criteria have self-assessments
                var criteria = await _dbContext.Criteria
                    .Where(c => c.TaskId == taskId)
                    .ToListAsync(cancellationToken);

                var providedCriterionIds = request.SelfAssessments.Select(s => s.CriterionId).ToHashSet();

                foreach (var criterion in criteria)
                {
                    if (!providedCriterionIds.Contains(criterion.Id))
                    {
                        return SubmissionAccessResult.Forbidden();
                    }
                }
            }

            var existingSubmission = await _dbContext.Submissions
                .Include(x => x.CriterionResults)
                .FirstOrDefaultAsync(x =>
                    x.assignmentId == taskId &&
                    x.authorId == authorId &&
                    x.status != SubmissionStatusEnum.Graded, cancellationToken);

            if (existingSubmission is not null)
            {
                if (existingSubmission.status != SubmissionStatusEnum.Draft)
                {
                    return SubmissionAccessResult.Success(MapToDto(existingSubmission));
                }

                // Update existing submission
                if (task.SelfAssessmentEnabled && request.SelfAssessments != null)
                {
                    existingSubmission.CriterionResults ??= new List<CriterionResult>();
                    existingSubmission.CriterionResults.Clear();

                    foreach (var selfAssessment in request.SelfAssessments)
                    {
                        existingSubmission.CriterionResults.Add(new CriterionResult
                        {
                            Id = Guid.NewGuid(),
                            SubmissionId = existingSubmission.id,
                            CriterionId = selfAssessment.CriterionId,
                            Value = selfAssessment.Value,
                            Comment = selfAssessment.Comment,
                            CreatedBy = authorId.ToString(),
                            AssessmentType = "SELF"
                        });
                    }
                }

                existingSubmission.submittedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return SubmissionAccessResult.Success(MapToDto(existingSubmission));
            }

            // Create new submission
            var submission = new Submission
            {
                id = Guid.NewGuid(),
                assignmentId = taskId,
                authorId = authorId,
                post = task,
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow
            };

            var criterionResults = new List<CriterionResult>();

            if (task.SelfAssessmentEnabled && request.SelfAssessments != null)
            {
                foreach (var selfAssessment in request.SelfAssessments)
                {
                    criterionResults.Add(new CriterionResult
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submission.id,
                        CriterionId = selfAssessment.CriterionId,
                        Value = selfAssessment.Value,
                        Comment = selfAssessment.Comment,
                        CreatedBy = authorId.ToString(),
                        AssessmentType = "SELF"
                    });
                }
            }

            submission.CriterionResults = criterionResults;

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
                .FirstOrDefaultAsync(x => x.id == submissionId);

            if (submission == null)
                return SubmissionAccessResult.NotFound();

            return SubmissionAccessResult.Success(MapToDto(submission));
        }

        public async Task<SubmissionAccessResult> PatchSubmission(
    Guid submissionId,
    SubmissionCreateRequest request)
        {
            var submission = await _dbContext.Submissions
                .Include(x => x.answers)
                .FirstOrDefaultAsync(x => x.id == submissionId);

            if (submission == null)
                return SubmissionAccessResult.NotFound();

            if (submission.status != SubmissionStatusEnum.Draft)
                return SubmissionAccessResult.Forbidden();

            submission.answers.Clear();

            foreach (var a in request.answers)
            {
                submission.answers.Add(new AnswerItem
                {
                    id = Guid.NewGuid(),
                    assignmentQuestionId = a.assignmentQuestionId,
                    answerType = a.answerType,
                    selectedOptionId = a.selectedOptionId,
                    selectedOptionsId = a.selectedOptionIds,
                    text = a.text
                });
            }

            await _dbContext.SaveChangesAsync();

            return SubmissionAccessResult.Success(MapToDto(submission));
        }

        public async Task<SubmissionAccessResult> SubmitSubmission(Guid submissionId)
        {
            var submission = await _dbContext.Submissions
                .FirstOrDefaultAsync(x => x.id == submissionId);

            if (submission == null)
                return SubmissionAccessResult.NotFound();

            if (submission.status != SubmissionStatusEnum.Draft)
                return SubmissionAccessResult.Forbidden();

            submission.status = SubmissionStatusEnum.RequiresReview;

            await _dbContext.SaveChangesAsync();

            return SubmissionAccessResult.Success(MapToDto(submission));
        }

        public async Task<SubmissionAccessResult> WithdrawSubmission(Guid submissionId)
        {
            var submission = await _dbContext.Submissions
                .FirstOrDefaultAsync(x => x.id == submissionId);

            if (submission == null)
                return SubmissionAccessResult.NotFound();

            if (submission.status != SubmissionStatusEnum.RequiresReview)
                return SubmissionAccessResult.Forbidden();

            submission.status = SubmissionStatusEnum.Draft;

            await _dbContext.SaveChangesAsync();

            return SubmissionAccessResult.Success(MapToDto(submission));
        }
        private SubmissionDto MapToDto(Submission submission)
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
                isFinalTeamDecision = submission.DecisionSession?.IsClosed == true
                    && submission.DecisionSession.Result == DecisionResult.Approved,
                answers = submission.answers?.Select(a => new AnswerItemDto
                {
                    id = a.id,
                    assignmentQuestionId = a.assignmentQuestionId,
                    answerType = a.answerType,
                    selectedOptionId = a.selectedOptionId,
                    selectedOptionIds = a.selectedOptionsId,
                    text = a.text
                }).ToList()
            };
        }
    }
}
