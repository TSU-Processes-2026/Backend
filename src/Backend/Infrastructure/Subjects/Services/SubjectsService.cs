using Application.Subjects.Contracts;
using Application.Subjects.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Subjects.Services;

public sealed class SubjectsService : ISubjectsService
{
    private const string AdminRole = "Admin";

    private readonly LmsDbContext _dbContext;

    public SubjectsService(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubjectResponse> CreateAsync(Guid currentUserId, CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Title = request.Title ?? string.Empty,
            Description = request.Description ?? string.Empty,
            Participants = new List<SubjectParticipant>()
        };

        var participant = new SubjectParticipant
        {
            SubjectId = subject.Id,
            UserId = currentUserId,
            Role = AdminRole,
            Subject = subject
        };

        _dbContext.Subjects.Add(subject);
        _dbContext.SubjectParticipants.Add(participant);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubjectResponse
        {
            Id = subject.Id,
            Title = subject.Title,
            Description = subject.Description
        };
    }

    public async Task<IReadOnlyList<SubjectResponse>> GetListAsync(Guid currentUserId, int limit, int offset, CancellationToken cancellationToken)
    {
        var safeLimit = limit <= 0 ? 20 : limit;
        var safeOffset = offset < 0 ? 0 : offset;

        return await _dbContext.SubjectParticipants
            .Where(x => x.UserId == currentUserId)
            .OrderBy(x => x.SubjectId)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(x => new SubjectResponse
            {
                Id = x.Subject.Id,
                Title = x.Subject.Title,
                Description = x.Subject.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SubjectAccessResult> GetByIdAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.Subjects
            .SingleOrDefaultAsync(x => x.Id == subjectId, cancellationToken);

        if (subject is null)
        {
            return SubjectAccessResult.NotFound();
        }

        var isParticipant = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == currentUserId, cancellationToken);

        if (!isParticipant)
        {
            return SubjectAccessResult.Forbidden();
        }

        return SubjectAccessResult.Success(MapSubject(subject));
    }

    public async Task<SubjectAccessResult> UpdateAsync(Guid currentUserId, Guid subjectId, UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.Subjects
            .SingleOrDefaultAsync(x => x.Id == subjectId, cancellationToken);

        if (subject is null)
        {
            return SubjectAccessResult.NotFound();
        }

        var isAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == currentUserId && x.Role == AdminRole, cancellationToken);

        if (!isAdmin)
        {
            return SubjectAccessResult.Forbidden();
        }

        subject.Title = request.Title ?? subject.Title;
        subject.Description = request.Description ?? subject.Description;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubjectAccessResult.Success(MapSubject(subject));
    }

    public async Task<SubjectDeleteResult> DeleteAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.Subjects
            .SingleOrDefaultAsync(x => x.Id == subjectId, cancellationToken);

        if (subject is null)
        {
            return SubjectDeleteResult.NotFound();
        }

        var isAdmin = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == currentUserId && x.Role == AdminRole, cancellationToken);

        if (!isAdmin)
        {
            return SubjectDeleteResult.Forbidden();
        }

        _dbContext.Subjects.Remove(subject);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubjectDeleteResult.Success();
    }

    private static SubjectResponse MapSubject(Subject subject)
    {
        return new SubjectResponse
        {
            Id = subject.Id,
            Title = subject.Title,
            Description = subject.Description
        };
    }
}
