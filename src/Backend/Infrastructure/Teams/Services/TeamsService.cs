using Application.Teams.Contracts;
using Application.Teams.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Teams.Services;

public sealed class TeamsService : ITeamsService
{
    private const string TeacherRole = "Teacher";
    private const string AdminRole = "Admin";
    private const string StudentRole = "Student";

    private readonly LmsDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public TeamsService(LmsDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<TeamSettingsResult> UpdateSettingsAsync(Guid currentUserId, Guid subjectId, TeamSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamSettingsResult.Forbidden();
        }

        var existing = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        var distributionMode = request.DistributionMode ?? existing?.DistributionMode ?? TeamDistributionMode.Manual;

        var snapshot = new SettingsSnapshot(
            distributionMode,
            request.FixedTeamsCount,
            request.FixedTeamSize,
            request.MinTeamSize,
            request.MaxTeamSize);

        var errors = ValidateSettings(snapshot);
        if (errors.Count > 0)
        {
            return TeamSettingsResult.Invalid(errors);
        }

        var settings = existing ?? new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = distributionMode,
            IsFinalized = false
        };

        settings.DistributionMode = distributionMode;
        settings.FixedTeamsCount = request.FixedTeamsCount;
        settings.FixedTeamSize = request.FixedTeamSize;
        settings.MinTeamSize = request.MinTeamSize;
        settings.MaxTeamSize = request.MaxTeamSize;
        settings.IsFinalized = false;
        settings.FinalizedAt = null;

        if (existing is null)
        {
            _dbContext.SubjectTeamSettings.Add(settings);
        }

        var studentCount = await GetStudentCountAsync(subjectId, cancellationToken);
        var warnings = GetFeasibilityWarnings(studentCount, snapshot);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return TeamSettingsResult.Success(MapSettings(settings, warnings));
    }

    public async Task<TeamListResult> GetTeamsAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamListResult.Forbidden();
        }

        var teams = await LoadTeamsAsync(subjectId, cancellationToken);

        return TeamListResult.Success(teams.Select(MapTeam).ToList());
    }

    public async Task<TeamValidationResult> ValidateManualDistributionAsync(Guid currentUserId, Guid subjectId, ManualTeamDistributionRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamValidationResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Manual)
        {
            return TeamValidationResult.Forbidden();
        }

        var snapshot = SettingsSnapshot.From(settings);
        var outcome = await ValidateManualTeamsAsync(subjectId, request.Teams, snapshot, cancellationToken);

        return TeamValidationResult.Success(new TeamValidationResponse
        {
            IsValid = outcome.Errors.Count == 0 && outcome.Warnings.Count == 0,
            Errors = outcome.Errors,
            Warnings = outcome.Warnings
        });
    }

    public async Task<TeamCreateResult> CreateManualDistributionAsync(Guid currentUserId, Guid subjectId, ManualTeamDistributionRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamCreateResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Manual)
        {
            return TeamCreateResult.Forbidden();
        }

        var snapshot = SettingsSnapshot.From(settings);
        var outcome = await ValidateManualTeamsAsync(subjectId, request.Teams, snapshot, cancellationToken);

        if (outcome.Errors.Count > 0)
        {
            return TeamCreateResult.Invalid(outcome.Errors);
        }

        var existingTeams = await _dbContext.Teams
            .Where(x => x.SubjectId == subjectId)
            .ToListAsync(cancellationToken);

        if (existingTeams.Count > 0)
        {
            _dbContext.Teams.RemoveRange(existingTeams);
        }

        var resultTeams = new List<TeamResponse>();

        foreach (var teamRequest in request.Teams)
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                CreatedAt = _timeProvider.GetUtcNow(),
                Subject = await _dbContext.Subjects.SingleAsync(x => x.Id == subjectId, cancellationToken)
            };

            team.Members = teamRequest.MemberIds
                .Select(memberId => new TeamMember
                {
                    Id = Guid.NewGuid(),
                    TeamId = team.Id,
                    UserId = memberId,
                    Team = team
                })
                .ToList();

            _dbContext.Teams.Add(team);

            resultTeams.Add(new TeamResponse
            {
                Id = team.Id,
                SubjectId = subjectId,
                MemberIds = teamRequest.MemberIds.ToList()
            });
        }

        var settingsEntity = settings ?? new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = TeamDistributionMode.Manual,
            IsFinalized = false
        };

        settingsEntity.IsFinalized = false;
        settingsEntity.FinalizedAt = null;

        if (settings is null)
        {
            _dbContext.SubjectTeamSettings.Add(settingsEntity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return TeamCreateResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams,
            Warnings = outcome.Warnings
        });
    }

    public async Task<TeamMutationResult> CreateTeamAsync(Guid currentUserId, Guid subjectId, ManualTeamRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamMutationResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Manual)
        {
            return TeamMutationResult.Forbidden();
        }

        var existingTeams = await LoadTeamsAsync(subjectId, cancellationToken);
        var manualTeams = existingTeams
            .Select(team => new ManualTeamRequest
            {
                MemberIds = team.Members.Select(member => member.UserId).ToList()
            })
            .ToList();

        manualTeams.Add(request);

        var snapshot = SettingsSnapshot.From(settings);
        var outcome = await ValidateManualTeamsAsync(subjectId, manualTeams, snapshot, cancellationToken);

        if (outcome.Errors.Count > 0)
        {
            return TeamMutationResult.Invalid(outcome.Errors);
        }

        var subject = await _dbContext.Subjects
            .SingleAsync(x => x.Id == subjectId, cancellationToken);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            CreatedAt = _timeProvider.GetUtcNow(),
            Subject = subject
        };

        team.Members = request.MemberIds
            .Select(memberId => new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = memberId,
                Team = team
            })
            .ToList();

        _dbContext.Teams.Add(team);

        var settingsEntity = settings ?? new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = TeamDistributionMode.Manual,
            IsFinalized = false
        };

        settingsEntity.IsFinalized = false;
        settingsEntity.FinalizedAt = null;

        if (settings is null)
        {
            _dbContext.SubjectTeamSettings.Add(settingsEntity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var resultTeams = await LoadTeamsAsync(subjectId, cancellationToken);

        return TeamMutationResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(MapTeam).ToList(),
            Warnings = outcome.Warnings
        });
    }

    public async Task<TeamMutationResult> UpdateTeamAsync(Guid currentUserId, Guid subjectId, Guid teamId, ManualTeamRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamMutationResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Manual)
        {
            return TeamMutationResult.Forbidden();
        }

        var existingTeams = await LoadTeamsAsync(subjectId, cancellationToken);
        var targetTeam = existingTeams.SingleOrDefault(team => team.Id == teamId);

        if (targetTeam is null)
        {
            return TeamMutationResult.Invalid(new[] { "Team not found." });
        }

        var manualTeams = existingTeams
            .Select(team => new ManualTeamRequest
            {
                MemberIds = team.Id == teamId
                    ? request.MemberIds.ToList()
                    : team.Members.Select(member => member.UserId).ToList()
            })
            .ToList();

        var snapshot = SettingsSnapshot.From(settings);
        var outcome = await ValidateManualTeamsAsync(subjectId, manualTeams, snapshot, cancellationToken);

        if (outcome.Errors.Count > 0)
        {
            return TeamMutationResult.Invalid(outcome.Errors);
        }

        _dbContext.TeamMembers.RemoveRange(targetTeam.Members);
        targetTeam.Members = request.MemberIds
            .Select(memberId => new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = targetTeam.Id,
                UserId = memberId,
                Team = targetTeam
            })
            .ToList();

        var settingsEntity = settings ?? new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = TeamDistributionMode.Manual,
            IsFinalized = false
        };

        settingsEntity.IsFinalized = false;
        settingsEntity.FinalizedAt = null;

        if (settings is null)
        {
            _dbContext.SubjectTeamSettings.Add(settingsEntity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var resultTeams = await LoadTeamsAsync(subjectId, cancellationToken);

        return TeamMutationResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(MapTeam).ToList(),
            Warnings = outcome.Warnings
        });
    }

    public async Task<TeamFinalizeResult> FinalizeAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamFinalizeResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Manual)
        {
            return TeamFinalizeResult.Forbidden();
        }

        var teams = await _dbContext.Teams
            .Include(x => x.Members)
            .Where(x => x.SubjectId == subjectId)
            .ToListAsync(cancellationToken);

        var manualTeams = teams
            .Select(team => new ManualTeamRequest
            {
                MemberIds = team.Members.Select(member => member.UserId).ToList()
            })
            .ToList();

        var snapshot = SettingsSnapshot.From(settings);
        var outcome = await ValidateManualTeamsAsync(subjectId, manualTeams, snapshot, cancellationToken);

        if (outcome.Errors.Count > 0 || outcome.Warnings.Count > 0)
        {
            return TeamFinalizeResult.Invalid(outcome.Errors, outcome.Warnings);
        }

        var settingsEntity = settings ?? new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = TeamDistributionMode.Manual,
            IsFinalized = false
        };

        settingsEntity.IsFinalized = true;
        settingsEntity.FinalizedAt = _timeProvider.GetUtcNow();

        if (settings is null)
        {
            _dbContext.SubjectTeamSettings.Add(settingsEntity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return TeamFinalizeResult.Success(new TeamFinalizeResponse
        {
            IsFinalized = settingsEntity.IsFinalized,
            FinalizedAt = settingsEntity.FinalizedAt
        });
    }

    private async Task<ValidationOutcome> ValidateManualTeamsAsync(
        Guid subjectId,
        IReadOnlyList<ManualTeamRequest> teams,
        SettingsSnapshot settings,
        CancellationToken cancellationToken)
    {
        var outcome = new ValidationOutcome();
        var errors = outcome.Errors;
        var warnings = outcome.Warnings;

        errors.AddRange(ValidateSettings(settings));

        if (teams is null || teams.Count == 0)
        {
            errors.Add("Teams list is required.");
            return outcome;
        }

        if (teams.Any(team => team.MemberIds is null || team.MemberIds.Count == 0))
        {
            errors.Add("Each team must contain at least one member.");
        }

        var studentIds = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        if (studentIds.Count == 0)
        {
            errors.Add("No students found in subject.");
            return outcome;
        }

        var assigned = new HashSet<Guid>();
        var duplicates = new HashSet<Guid>();

        foreach (var team in teams)
        {
            foreach (var memberId in team.MemberIds)
            {
                if (memberId == Guid.Empty)
                {
                    errors.Add("Team member id must be a valid guid.");
                    continue;
                }

                if (!assigned.Add(memberId))
                {
                    duplicates.Add(memberId);
                }
            }
        }

        if (duplicates.Count > 0)
        {
            errors.Add("Each student must belong to only one team.");
        }

        var unknownStudents = assigned.Except(studentIds).ToList();
        if (unknownStudents.Count > 0)
        {
            errors.Add("All team members must be students of the subject.");
        }

        var missingStudents = studentIds.Except(assigned).ToList();
        if (missingStudents.Count > 0)
        {
            warnings.Add("All students must be assigned to a team.");
        }

        warnings.AddRange(GetConstraintWarnings(studentIds.Count, teams, settings));

        return outcome;
    }

    private static List<string> ValidateSettings(SettingsSnapshot settings)
    {
        var errors = new List<string>();

        if (settings.FixedTeamsCount.HasValue && settings.FixedTeamsCount.Value <= 0)
        {
            errors.Add("FixedTeamsCount must be greater than zero.");
        }

        if (settings.FixedTeamSize.HasValue && settings.FixedTeamSize.Value <= 0)
        {
            errors.Add("FixedTeamSize must be greater than zero.");
        }

        if (settings.MinTeamSize.HasValue && settings.MinTeamSize.Value <= 0)
        {
            errors.Add("MinTeamSize must be greater than zero.");
        }

        if (settings.MaxTeamSize.HasValue && settings.MaxTeamSize.Value <= 0)
        {
            errors.Add("MaxTeamSize must be greater than zero.");
        }

        if (settings.MinTeamSize.HasValue && settings.MaxTeamSize.HasValue && settings.MinTeamSize.Value > settings.MaxTeamSize.Value)
        {
            errors.Add("MinTeamSize must be less than or equal to MaxTeamSize.");
        }

        return errors;
    }

    private static List<string> GetConstraintWarnings(int totalStudents, IReadOnlyList<ManualTeamRequest> teams, SettingsSnapshot settings)
    {
        var warnings = new List<string>();

        if (settings.FixedTeamsCount.HasValue && teams.Count != settings.FixedTeamsCount.Value)
        {
            warnings.Add("Teams count does not match FixedTeamsCount.");
        }

        if (settings.FixedTeamSize.HasValue)
        {
            var size = settings.FixedTeamSize.Value;

            if (totalStudents % size != 0)
            {
                warnings.Add("Total number of students must be divisible by FixedTeamSize.");
            }

            if (teams.Any(team => team.MemberIds.Count != size))
            {
                warnings.Add("All teams must have exactly FixedTeamSize members.");
            }
        }

        if (settings.MinTeamSize.HasValue || settings.MaxTeamSize.HasValue)
        {
            var min = settings.MinTeamSize ?? 1;
            var max = settings.MaxTeamSize ?? int.MaxValue;

            if (teams.Any(team => team.MemberIds.Count < min || team.MemberIds.Count > max))
            {
                warnings.Add("All teams must be within MinTeamSize and MaxTeamSize.");
            }
        }

        warnings.AddRange(GetFeasibilityWarnings(totalStudents, settings));

        return warnings;
    }

    private static List<string> GetFeasibilityWarnings(int totalStudents, SettingsSnapshot settings)
    {
        var warnings = new List<string>();

        if (totalStudents <= 0)
        {
            return warnings;
        }

        if (settings.FixedTeamSize.HasValue && totalStudents % settings.FixedTeamSize.Value != 0)
        {
            warnings.Add("Total number of students cannot be evenly divided by FixedTeamSize.");
        }

        if (settings.FixedTeamsCount.HasValue)
        {
            var teams = settings.FixedTeamsCount.Value;

            if (teams > totalStudents)
            {
                warnings.Add("FixedTeamsCount exceeds total number of students.");
            }

            if (settings.FixedTeamSize.HasValue)
            {
                var expected = teams * settings.FixedTeamSize.Value;
                if (expected != totalStudents)
                {
                    warnings.Add("Total number of students must be equal to FixedTeamsCount multiplied by FixedTeamSize.");
                }
            }
            else if (settings.MinTeamSize.HasValue || settings.MaxTeamSize.HasValue)
            {
                var min = settings.MinTeamSize ?? 1;
                var max = settings.MaxTeamSize ?? int.MaxValue;
                if (totalStudents < teams * min || totalStudents > teams * max)
                {
                    warnings.Add("Total number of students does not fit FixedTeamsCount with MinTeamSize and MaxTeamSize.");
                }
            }
        }
        else if (settings.MinTeamSize.HasValue || settings.MaxTeamSize.HasValue)
        {
            var min = settings.MinTeamSize ?? 1;
            var max = settings.MaxTeamSize ?? int.MaxValue;

            var minTeams = (int)Math.Ceiling(totalStudents / (double)max);
            var maxTeams = totalStudents / min;

            if (minTeams > maxTeams)
            {
                warnings.Add("Current number of students cannot be distributed within MinTeamSize and MaxTeamSize.");
            }
        }

        return warnings;
    }

    private async Task<int> GetStudentCountAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .CountAsync(cancellationToken);
    }

    private static TeamSettingsResponse MapSettings(SubjectTeamSettings settings, IReadOnlyList<string> warnings)
    {
        return new TeamSettingsResponse
        {
            SubjectId = settings.SubjectId,
            DistributionMode = settings.DistributionMode,
            FixedTeamsCount = settings.FixedTeamsCount,
            FixedTeamSize = settings.FixedTeamSize,
            MinTeamSize = settings.MinTeamSize,
            MaxTeamSize = settings.MaxTeamSize,
            IsFinalized = settings.IsFinalized,
            FinalizedAt = settings.FinalizedAt,
            Warnings = warnings
        };
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

    private async Task<bool> IsParticipantAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId, cancellationToken);
    }

    private async Task<List<Team>> LoadTeamsAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .Include(x => x.Members)
            .Where(x => x.SubjectId == subjectId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private static TeamResponse MapTeam(Team team)
    {
        return new TeamResponse
        {
            Id = team.Id,
            SubjectId = team.SubjectId,
            MemberIds = team.Members.Select(member => member.UserId).ToList()
        };
    }

    private sealed class ValidationOutcome
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    private readonly record struct SettingsSnapshot(
        TeamDistributionMode DistributionMode,
        int? FixedTeamsCount,
        int? FixedTeamSize,
        int? MinTeamSize,
        int? MaxTeamSize)
    {
        public static SettingsSnapshot From(SubjectTeamSettings? settings)
        {
            if (settings is null)
            {
                return new SettingsSnapshot(TeamDistributionMode.Manual, null, null, null, null);
            }

            return new SettingsSnapshot(
                settings.DistributionMode,
                settings.FixedTeamsCount,
                settings.FixedTeamSize,
                settings.MinTeamSize,
                settings.MaxTeamSize);
        }
    }
}
