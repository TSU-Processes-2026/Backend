using Application.Assignments.Contracts;
using Application.Assignments.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Assignments.Services;

public sealed class AssignmentsService : IAssignmentsService
{
    private const string AssignmentPostType = "Assignment";
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AssignmentsService(LmsDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<AssignmentListResult> GetSubjectAssignmentsAsync(Guid currentUserId, Guid subjectId, int limit, int offset, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return AssignmentListResult.Forbidden();
        }

        var safeLimit = limit <= 0 ? 20 : limit;
        var safeOffset = offset < 0 ? 0 : offset;

        var assignments = await _dbContext.Posts
            .Where(x => x.SubjectId == subjectId && x.PostType == AssignmentPostType)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .ToListAsync(cancellationToken);

        return AssignmentListResult.Success(assignments.Select(MapAssignment).ToList());
    }

    public async Task<AssignmentAccessResult> GetByIdAsync(Guid currentUserId, Guid assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.Posts
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.PostType == AssignmentPostType, cancellationToken);

        if (assignment is null)
        {
            return AssignmentAccessResult.Forbidden();
        }

        if (!await IsParticipantAsync(currentUserId, assignment.SubjectId, cancellationToken))
        {
            return AssignmentAccessResult.Forbidden();
        }

        return AssignmentAccessResult.Success(MapAssignment(assignment));
    }

    public async Task<AssignmentUpdateResult> CreateAsync(Guid currentUserId, Guid subjectId, UpsertAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return AssignmentUpdateResult.Forbidden();
        }

        var assignment = new Post
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            AuthorId = currentUserId,
            PostType = AssignmentPostType,
            Content = request.Content ?? string.Empty,
            AssignmentData = request.AssignmentData ?? string.Empty,
            CreatedAt = _timeProvider.GetUtcNow(),
            Subject = await _dbContext.Subjects.SingleAsync(x => x.Id == subjectId, cancellationToken),
            Questions = BuildQuestions(request.Questions)
        };

        _dbContext.Posts.Add(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return AssignmentUpdateResult.Success(MapAssignment(assignment));
    }

    public async Task<AssignmentUpdateResult> UpdateAsync(Guid currentUserId, Guid assignmentId, UpsertAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.Posts
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.PostType == AssignmentPostType, cancellationToken);

        if (assignment is null)
        {
            return AssignmentUpdateResult.Forbidden();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, assignment.SubjectId, cancellationToken))
        {
            return AssignmentUpdateResult.Forbidden();
        }

        assignment.Content = request.Content ?? assignment.Content;
        assignment.AssignmentData = request.AssignmentData ?? assignment.AssignmentData ?? string.Empty;

        _dbContext.AssignmentQuestionOptions.RemoveRange(assignment.Questions.SelectMany(x => x.Options));
        _dbContext.AssignmentQuestions.RemoveRange(assignment.Questions);
        assignment.Questions = BuildQuestions(request.Questions);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return AssignmentUpdateResult.Success(MapAssignment(assignment));
    }

    public async Task<AssignmentDeleteResult> DeleteAsync(Guid currentUserId, Guid assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.Posts
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.PostType == AssignmentPostType, cancellationToken);

        if (assignment is null)
        {
            return AssignmentDeleteResult.Forbidden();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, assignment.SubjectId, cancellationToken))
        {
            return AssignmentDeleteResult.Forbidden();
        }

        _dbContext.Posts.Remove(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return AssignmentDeleteResult.Success();
    }

    private async Task<bool> IsParticipantAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId, cancellationToken);
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

    private static List<AssignmentQuestion> BuildQuestions(IReadOnlyList<AssignmentQuestionRequest>? requestQuestions)
    {
        if (requestQuestions is null)
        {
            return new List<AssignmentQuestion>();
        }

        return requestQuestions.Select(question =>
        {
            var questionId = question.Id ?? Guid.NewGuid();
            var options = (question.Options ?? Array.Empty<AssignmentQuestionOptionRequest>())
                .Select(option => new AssignmentQuestionOption
                {
                    Id = option.Id ?? Guid.NewGuid(),
                    QuestionId = questionId,
                    Text = option.Text ?? string.Empty
                })
                .ToList();

            return new AssignmentQuestion
            {
                Id = questionId,
                QuestionType = question.QuestionType ?? string.Empty,
                QuestionData = question.QuestionData ?? string.Empty,
                Options = options
            };
        }).ToList();
    }

    private static AssignmentResponse MapAssignment(Post assignment)
    {
        return new AssignmentResponse
        {
            Id = assignment.Id,
            SubjectId = assignment.SubjectId,
            AuthorId = assignment.AuthorId,
            PostType = AssignmentPostType,
            Content = assignment.Content,
            CreatedAt = assignment.CreatedAt,
            AssignmentData = assignment.AssignmentData ?? string.Empty,
            Questions = assignment.Questions
                .OrderBy(x => x.Id)
                .Select(x => new AssignmentQuestionResponse
                {
                    Id = x.Id,
                    QuestionType = x.QuestionType,
                    QuestionData = x.QuestionData,
                    Options = x.Options
                        .OrderBy(y => y.Id)
                        .Select(y => new AssignmentQuestionOptionResponse
                        {
                            Id = y.Id,
                            Text = y.Text
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
