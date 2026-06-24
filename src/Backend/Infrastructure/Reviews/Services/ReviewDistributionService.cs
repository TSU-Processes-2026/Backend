using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Infrastructure.Reviews.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reviews.Services;

public sealed class ReviewDistributionService : IReviewDistributionService
{
    private readonly LmsDbContext _dbContext;

    public ReviewDistributionService(LmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ReviewAssignment>> GenerateAllToAllAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == taskId, ct);

        if (task is null)
            return Array.Empty<ReviewAssignment>();

        if (task.ReviewType == "team")
            return await GenerateTeamAllToAllAsync(task, ct);

        var submissions = await _dbContext.Submissions
            .Where(x => x.assignmentId == taskId)
            .ToListAsync(ct);

        var assignments = new List<ReviewAssignment>();

        foreach (var submission in submissions)
        {
            foreach (var other in submissions)
            {
                if (submission.id == other.id)
                    continue;

                if (submission.authorId == other.authorId)
                    continue;

                assignments.Add(new ReviewAssignment
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    SubmissionId = submission.id,
                    ReviewerUserId = other.authorId,
                    ReviewTargetType = submission.SubmissionType ?? "submission",
                    Status = "pending",
                    AssignedAt = DateTimeOffset.UtcNow,
                    StartsAt = DateTimeOffset.UtcNow,
                    DueAt = task.ReviewDeadlineAt ?? DateTimeOffset.UtcNow.AddDays(7)
                });
            }
        }

        if (assignments.Count > 0)
        {
            _dbContext.ReviewAssignments.AddRange(assignments);
            await _dbContext.SaveChangesAsync(ct);
        }

        return assignments;
    }

    private async Task<IReadOnlyList<ReviewAssignment>> GenerateTeamAllToAllAsync(Post task, CancellationToken ct)
    {
        var teamGrades = await _dbContext.TeamGrades
            .Where(x => x.AssignmentId == task.Id)
            .Include(x => x.Team)
                .ThenInclude(x => x.Members)
            .ToListAsync(ct);

        if (teamGrades.Count < 2)
            return Array.Empty<ReviewAssignment>();

        var assignments = new List<ReviewAssignment>();
        var policy = task.TeamReviewPolicy ?? "all_members";

        foreach (var targetTeamGrade in teamGrades)
        {
            foreach (var reviewerTeamGrade in teamGrades)
            {
                if (targetTeamGrade.TeamId == reviewerTeamGrade.TeamId)
                    continue;

                assignments.AddRange(CreateTeamReviewAssignments(
                    task, targetTeamGrade, reviewerTeamGrade, policy));
            }
        }

        if (assignments.Count > 0)
        {
            _dbContext.ReviewAssignments.AddRange(assignments);
            await _dbContext.SaveChangesAsync(ct);
        }

        return assignments;
    }

    public async Task<IReadOnlyList<ReviewAssignment>> GeneratePairsAsync(Guid taskId, string pairingStrategy, CancellationToken ct)
    {
        var task = await _dbContext.Posts
            .Include(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == taskId, ct);

        if (task is null)
            return Array.Empty<ReviewAssignment>();

        if (task.ReviewType == "team")
            return await GenerateTeamPairsAsync(task, ct);

        var submissions = await _dbContext.Submissions
            .Where(x => x.assignmentId == taskId)
            .ToListAsync(ct);

        if (submissions.Count < 2)
            return Array.Empty<ReviewAssignment>();

        submissions = submissions
            .OrderBy(x => x.DefenseOrder)
            .ThenBy(x => x.submittedAt)
            .ToList();

        var assignments = new List<ReviewAssignment>();
        var n = submissions.Count;

        // For odd n >= 3, form a triplet with the last 3 submissions (spec §5.2)
        if (n >= 3 && n % 2 == 1)
        {
            // Circular pairs for first n-3 items
            for (var i = 0; i < n - 3; i++)
            {
                var current = submissions[i];
                var next = submissions[i + 1];

                if (current.authorId != next.authorId)
                    assignments.Add(MakeAssignment(taskId, task, current, next.authorId));
            }

            // Triplet for the last 3: each reviews the next in the triplet
            var a = submissions[n - 3];
            var b = submissions[n - 2];
            var c = submissions[n - 1];

            if (a.authorId != b.authorId)
                assignments.Add(MakeAssignment(taskId, task, a, b.authorId));
            if (b.authorId != c.authorId)
                assignments.Add(MakeAssignment(taskId, task, b, c.authorId));
            if (c.authorId != a.authorId)
                assignments.Add(MakeAssignment(taskId, task, c, a.authorId));
        }
        else
        {
            for (var i = 0; i < n; i++)
            {
                var current = submissions[i];
                var next = submissions[(i + 1) % n];

                if (current.authorId != next.authorId)
                    assignments.Add(MakeAssignment(taskId, task, current, next.authorId));
            }
        }

        if (assignments.Count > 0)
        {
            _dbContext.ReviewAssignments.AddRange(assignments);
            await _dbContext.SaveChangesAsync(ct);
        }

        return assignments;
    }

    private async Task<IReadOnlyList<ReviewAssignment>> GenerateTeamPairsAsync(Post task, CancellationToken ct)
    {
        var teamGrades = await _dbContext.TeamGrades
            .Where(x => x.AssignmentId == task.Id)
            .Include(x => x.Team)
                .ThenInclude(x => x.Members)
            .ToListAsync(ct);

        if (teamGrades.Count < 2)
            return Array.Empty<ReviewAssignment>();

        var assignments = new List<ReviewAssignment>();
        var policy = task.TeamReviewPolicy ?? "all_members";
        var n = teamGrades.Count;

        for (var i = 0; i < n; i++)
        {
            var current = teamGrades[i];
            var next = teamGrades[(i + 1) % n];

            assignments.AddRange(CreateTeamReviewAssignments(
                task, current, next, policy));
        }

        if (assignments.Count > 0)
        {
            _dbContext.ReviewAssignments.AddRange(assignments);
            await _dbContext.SaveChangesAsync(ct);
        }

        return assignments;
    }

    private static List<ReviewAssignment> CreateTeamReviewAssignments(
        Post task, TeamGrade target, TeamGrade reviewer, string policy)
    {
        var assignments = new List<ReviewAssignment>();

        switch (policy)
        {
            case "captain_only":
                if (reviewer.Team.CaptainUserId is not null)
                {
                    assignments.Add(MakeTeamAssignment(task, target, reviewer.Team.CaptainUserId.Value, reviewer.TeamId));
                }
                break;
            case "one_representative_reviews":
                var reviewerUserId = reviewer.Team.RepresentativeUserId ?? reviewer.Team.CaptainUserId;
                if (reviewerUserId is not null)
                {
                    assignments.Add(MakeTeamAssignment(task, target, reviewerUserId.Value, reviewer.TeamId));
                }
                break;
            case "all_members":
            default:
                foreach (var member in reviewer.Team.Members)
                {
                    assignments.Add(MakeTeamAssignment(task, target, member.UserId, reviewer.TeamId));
                }
                break;
        }

        return assignments;
    }

    private static ReviewAssignment MakeTeamAssignment(
        Post task, TeamGrade target, Guid reviewerUserId, Guid reviewerTeamId)
    {
        return new ReviewAssignment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            SubmissionId = target.SubmissionId,
            ReviewerUserId = reviewerUserId,
            ReviewerTeamId = reviewerTeamId,
            ReviewTargetType = "team_submission",
            Status = "pending",
            AssignedAt = DateTimeOffset.UtcNow,
            StartsAt = DateTimeOffset.UtcNow,
            DueAt = task.ReviewDeadlineAt ?? DateTimeOffset.UtcNow.AddDays(7)
        };
    }

    private static ReviewAssignment MakeAssignment(Guid taskId, Post task, Submission submission, Guid reviewerUserId)
    {
        return new ReviewAssignment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            SubmissionId = submission.id,
            ReviewerUserId = reviewerUserId,
            ReviewTargetType = submission.SubmissionType ?? "submission",
            Status = "pending",
            AssignedAt = DateTimeOffset.UtcNow,
            StartsAt = DateTimeOffset.UtcNow,
            DueAt = task.ReviewDeadlineAt ?? DateTimeOffset.UtcNow.AddDays(7)
        };
    }
}
