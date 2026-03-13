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

            var answers = request.answers.Select(a => new AnswerItem
            {
                id = Guid.NewGuid(),
                assignmentQuestionId = a.assignmentQuestionId,
                answerType = a.answerType,
                selectedOptionId = a.selectedOptionId,
                selectedOptionsId = a.selectedOptionIds,
                text = a.text
            }).ToList();

            var submission = new Submission
            {
                id = Guid.NewGuid(),
                assignmentId = assignmentId,
                authorId = authorId,
                answers = answers,
                status = SubmissionStatusEnum.Draft,
                submittedAt = DateTime.UtcNow
            };

            _dbContext.Submissions.Add(submission);
            await _dbContext.SaveChangesAsync();

            return SubmissionAccessResult.Success(MapToDto(submission));
        }

        public async Task<List<SubmissionDto>> GetSubmissions(Guid assignmentId, int limit, int offset)
        {
            var submissions = await _dbContext.Submissions
                .Where(x => x.assignmentId == assignmentId)
                .Include(x => x.answers)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            return submissions.Select(MapToDto).ToList();
        }

        public async Task<SubmissionAccessResult> GetSubmission(Guid submissionId)
        {
            var submission = await _dbContext.Submissions
                .Include(x => x.answers)
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