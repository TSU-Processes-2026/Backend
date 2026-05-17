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
    private const string AssignmentPostType = "Assignment";

    private readonly LmsDbContext _dbContext;

    public CriteriaService(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CriterionUpdateResult> UpdateAsync(Guid currentUserId, Guid criterionId, UpdateCriterionRequest request, CancellationToken cancellationToken)
    {
        var criterion = await _dbContext.Criteria
            .Include(x => x.Task)
            .SingleOrDefaultAsync(x => x.Id == criterionId, cancellationToken);

        if (criterion is null)
        {
            return CriterionUpdateResult.NotFound();
        }

        if (!await IsTeacherOrAdminAsync(currentUserId, criterion.Task.SubjectId, cancellationToken))
        {
            return CriterionUpdateResult.Forbidden();
        }

        if (request.Description is not null)
        {
            criterion.Description = request.Description;
        }

        if (request.Format is not null)
        {
            criterion.Format = request.Format;
        }

        if (request.Weight.HasValue)
        {
            criterion.Weight = request.Weight.Value;
        }

        if (request.MaxPoints.HasValue)
        {
            criterion.MaxPoints = request.MaxPoints.Value;
        }

        if (request.Points.HasValue)
        {
            criterion.Points = request.Points.Value;
        }

        if (request.IsBonus.HasValue)
        {
            criterion.IsBonus = request.IsBonus.Value;
        }

        if (request.IsPenalty.HasValue)
        {
            criterion.IsPenalty = request.IsPenalty.Value;
        }

        if (request.Order.HasValue)
        {
            criterion.Order = request.Order.Value;
        }

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

    private async Task<bool> IsTeacherOrAdminAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(
                x => x.SubjectId == subjectId
                     && x.UserId == userId
                     && (x.Role == TeacherRole || x.Role == AdminRole),
                cancellationToken);
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
}
