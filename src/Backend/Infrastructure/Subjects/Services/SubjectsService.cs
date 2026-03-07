using Application.Subjects.Contracts;
using Application.Subjects.Models;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Subjects.Services;

public sealed class SubjectsService : ISubjectsService
{
    private const string AdminRole = "Admin";
    private const string StudentRole = "Student";
    private const string TeacherRole = "Teacher";

    private readonly LmsDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubjectsService(LmsDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
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

        return MapSubject(subject);
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

        var isAdmin = await IsAdminAsync(currentUserId, subjectId, cancellationToken);

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

        var isAdmin = await IsAdminAsync(currentUserId, subjectId, cancellationToken);

        if (!isAdmin)
        {
            return SubjectDeleteResult.Forbidden();
        }

        _dbContext.Subjects.Remove(subject);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubjectDeleteResult.Success();
    }

    public async Task<JoinSubjectResult> JoinAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        var subjectExists = await _dbContext.Subjects
            .AnyAsync(x => x.Id == subjectId, cancellationToken);

        if (!subjectExists)
        {
            return JoinSubjectResult.NotFound();
        }

        var existingParticipant = await _dbContext.SubjectParticipants
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId && x.UserId == currentUserId, cancellationToken);

        if (existingParticipant is not null)
        {
            return JoinSubjectResult.Success(MapParticipant(existingParticipant));
        }

        var participant = new SubjectParticipant
        {
            SubjectId = subjectId,
            UserId = currentUserId,
            Role = StudentRole,
            Subject = await _dbContext.Subjects.SingleAsync(x => x.Id == subjectId, cancellationToken)
        };

        _dbContext.SubjectParticipants.Add(participant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return JoinSubjectResult.Success(MapParticipant(participant));
    }

    public async Task<ParticipantMutationResult> AddParticipantAsync(Guid currentUserId, Guid subjectId, AddParticipantRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId is null || !IsMutableRole(request.Role))
        {
            return ParticipantMutationResult.Forbidden();
        }

        var isAdmin = await IsAdminAsync(currentUserId, subjectId, cancellationToken);

        if (!isAdmin)
        {
            return ParticipantMutationResult.Forbidden();
        }

        var user = await _userManager.FindByIdAsync(request.UserId.Value.ToString());

        if (user is null)
        {
            return ParticipantMutationResult.Forbidden();
        }

        var existing = await _dbContext.SubjectParticipants
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId && x.UserId == request.UserId.Value, cancellationToken);

        if (existing is null)
        {
            var subject = await _dbContext.Subjects
                .SingleOrDefaultAsync(x => x.Id == subjectId, cancellationToken);

            if (subject is null)
            {
                return ParticipantMutationResult.Forbidden();
            }

            existing = new SubjectParticipant
            {
                SubjectId = subjectId,
                UserId = request.UserId.Value,
                Role = request.Role!,
                Subject = subject
            };

            _dbContext.SubjectParticipants.Add(existing);
        }
        else
        {
            existing.Role = request.Role!;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ParticipantMutationResult.Success(MapParticipant(existing));
    }

    public async Task<ParticipantsListResult> GetParticipantsAsync(Guid currentUserId, Guid subjectId, int limit, int offset, CancellationToken cancellationToken)
    {
        var isParticipant = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == currentUserId, cancellationToken);

        if (!isParticipant)
        {
            return ParticipantsListResult.Forbidden();
        }

        var safeLimit = limit <= 0 ? 50 : limit;
        var safeOffset = offset < 0 ? 0 : offset;

        var participants = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId)
            .OrderBy(x => x.UserId)
            .Skip(safeOffset)
            .Take(safeLimit)
            .Select(x => new ParticipantResponse
            {
                UserId = x.UserId,
                Role = x.Role
            })
            .ToListAsync(cancellationToken);

        return ParticipantsListResult.Success(participants);
    }

    public async Task<ParticipantMutationResult> UpdateParticipantRoleAsync(Guid currentUserId, Guid subjectId, Guid targetUserId, UpdateParticipantRoleRequest request, CancellationToken cancellationToken)
    {
        if (!IsMutableRole(request.Role))
        {
            return ParticipantMutationResult.Forbidden();
        }

        var isAdmin = await IsAdminAsync(currentUserId, subjectId, cancellationToken);

        if (!isAdmin)
        {
            return ParticipantMutationResult.Forbidden();
        }

        var participant = await _dbContext.SubjectParticipants
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId && x.UserId == targetUserId, cancellationToken);

        if (participant is null)
        {
            return ParticipantMutationResult.Forbidden();
        }

        participant.Role = request.Role!;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ParticipantMutationResult.Success(MapParticipant(participant));
    }

    public async Task<ParticipantDeleteResult> DeleteParticipantAsync(Guid currentUserId, Guid subjectId, Guid targetUserId, CancellationToken cancellationToken)
    {
        var isAdmin = await IsAdminAsync(currentUserId, subjectId, cancellationToken);

        if (!isAdmin)
        {
            return ParticipantDeleteResult.Forbidden();
        }

        var participant = await _dbContext.SubjectParticipants
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId && x.UserId == targetUserId, cancellationToken);

        if (participant is null)
        {
            return ParticipantDeleteResult.Forbidden();
        }

        _dbContext.SubjectParticipants.Remove(participant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ParticipantDeleteResult.Success();
    }

    private async Task<bool> IsAdminAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId && x.Role == AdminRole, cancellationToken);
    }

    private static bool IsMutableRole(string? role)
    {
        return string.Equals(role, StudentRole, StringComparison.Ordinal) || string.Equals(role, TeacherRole, StringComparison.Ordinal);
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

    private static ParticipantResponse MapParticipant(SubjectParticipant participant)
    {
        return new ParticipantResponse
        {
            UserId = participant.UserId,
            Role = participant.Role
        };
    }
}
