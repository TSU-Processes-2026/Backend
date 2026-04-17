using System.Text.Json;
using Application.Submissions.Models;
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

    public async Task<TeamSettingsResult> GetSettingsAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamSettingsResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        var snapshot = SettingsSnapshot.From(settings);
        var errors = ValidateSettings(snapshot);

        if (errors.Count > 0)
        {
            return TeamSettingsResult.Invalid(errors);
        }

        var studentCount = await GetStudentCountAsync(subjectId, cancellationToken);
        var warnings = GetFeasibilityWarnings(studentCount, snapshot);

        if (settings is null)
        {
            return TeamSettingsResult.Success(new TeamSettingsResponse
            {
                SubjectId = subjectId,
                DistributionMode = snapshot.DistributionMode,
                FixedTeamsCount = snapshot.FixedTeamsCount,
                FixedTeamSize = snapshot.FixedTeamSize,
                MinTeamSize = snapshot.MinTeamSize,
                MaxTeamSize = snapshot.MaxTeamSize,
                IsFinalized = false,
                FinalizedAt = null,
                CaptainSelectionMode = snapshot.CaptainSelectionMode,
                CaptainVotingDeadlineDays = snapshot.CaptainVotingDeadlineDays,
                RequiresCaptain = snapshot.RequiresCaptain,
                DecisionMode = snapshot.DecisionMode,
                DecisionDeadlineDays = snapshot.DecisionDeadlineDays,
                RequiredDecisionVotes = snapshot.RequiredDecisionVotes,
                RequiresDecision = snapshot.RequiresDecision,
                Warnings = warnings
            });
        }

        return TeamSettingsResult.Success(MapSettings(settings, warnings));
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
        var oldMode = existing?.DistributionMode;

        var requiresCaptain = request.RequiresCaptain ?? existing?.RequiresCaptain ?? false;
        var captainSelectionMode = request.CaptainSelectionMode ?? existing?.CaptainSelectionMode;
        var captainVotingDeadlineDays = request.CaptainVotingDeadlineDays ?? existing?.CaptainVotingDeadlineDays;
        if (!requiresCaptain)
        {
            captainSelectionMode = null;
            captainVotingDeadlineDays = null;
        }

        var requiresDecision = request.RequiresDecision ?? existing?.RequiresDecision ?? false;
        SubmissionDecisionMode? decisionMode =
            request.DecisionMode ?? existing?.DecisionMode ?? SubmissionDecisionMode.Voting;
        var decisionDeadlineDays = request.DecisionDeadlineDays ?? existing?.DecisionDeadlineDays;
        var requiredDecisionVotes = request.RequiredDecisionVotes ?? existing?.RequiredDecisionVotes;
        if (!requiresDecision)
        {
            decisionMode = null;
            decisionDeadlineDays = null;
            requiredDecisionVotes = null;
        }

        var snapshot = new SettingsSnapshot(
            distributionMode,
            request.FixedTeamsCount,
            request.FixedTeamSize,
            request.MinTeamSize,
            request.MaxTeamSize,
            captainSelectionMode,
            captainVotingDeadlineDays,
            requiresCaptain,
            decisionMode,
            decisionDeadlineDays,
            requiredDecisionVotes,
            requiresDecision);

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
        settings.CaptainSelectionMode = captainSelectionMode;
        settings.CaptainVotingDeadlineDays = captainVotingDeadlineDays;
        settings.RequiresCaptain = requiresCaptain;
        settings.DecisionMode = decisionMode;
        settings.DecisionDeadlineDays = decisionDeadlineDays;
        settings.RequiredDecisionVotes = requiredDecisionVotes;
        settings.RequiresDecision = requiresDecision;
        settings.IsFinalized = false;
        settings.FinalizedAt = null;

        if (existing is null)
        {
            _dbContext.SubjectTeamSettings.Add(settings);
        }

        // When mode changes away from Draft, deactivate any existing draft state
        // so a new draft can be started later if mode switches back to Draft
        if (oldMode == TeamDistributionMode.Draft && distributionMode != TeamDistributionMode.Draft)
        {
            var existingDraft = await _dbContext.DraftStates
                .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

            if (existingDraft is not null)
            {
                existingDraft.IsActive = false;
                existingDraft.IsCompleted = true;
                existingDraft.CompletedAt = _timeProvider.GetUtcNow();
            }
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

        var usernames = await LoadUsernamesAsync(
            teams.SelectMany(team => team.Members.Select(member => member.UserId)),
            cancellationToken);

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        var distributionMode = settings?.DistributionMode ?? TeamDistributionMode.Manual;
        var isFinalized = settings?.IsFinalized ?? false;

        return TeamListResult.Success(
            teams.Select(team => MapTeam(team, usernames)).ToList(),
            distributionMode,
            isFinalized);
    }

    public async Task<UnassignedStudentsResult> GetUnassignedStudentsAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return UnassignedStudentsResult.Forbidden();
        }

        var studentIds = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        if (studentIds.Count == 0)
        {
            return UnassignedStudentsResult.Success(new UnassignedStudentsResponse
            {
                SubjectId = subjectId,
                StudentIds = Array.Empty<Guid>(),
                Students = Array.Empty<TeamMemberResponse>()
            });
        }

        var assignedIds = await _dbContext.TeamMembers
            .Where(x => x.Team.SubjectId == subjectId)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var unassignedIds = studentIds
            .Except(assignedIds)
            .ToList();

        var unassigned = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole && unassignedIds.Contains(x.UserId))
            .Join(
                _dbContext.Users,
                participant => participant.UserId,
                user => user.Id,
                (participant, user) => new TeamMemberResponse
                {
                    UserId = participant.UserId,
                    Username = user.UserName ?? string.Empty
                })
            .OrderBy(x => x.UserId)
            .ToListAsync(cancellationToken);

        return UnassignedStudentsResult.Success(new UnassignedStudentsResponse
        {
            SubjectId = subjectId,
            StudentIds = unassignedIds,
            Students = unassigned
        });
    }

    public async Task<TeamRandomPreviewResult> PreviewRandomDistributionAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamRandomPreviewResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Random)
        {
            return TeamRandomPreviewResult.Forbidden();
        }

        var snapshot = settings is null
            ? new SettingsSnapshot(TeamDistributionMode.Random, null, null, null, null, null, null, false, null, null, null, false)
            : SettingsSnapshot.From(settings);

        var students = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        if (students.Count == 0)
        {
            return TeamRandomPreviewResult.Invalid(BuildInvalidRandomPreviewResponse(subjectId, snapshot, 0, new[] { "No students found in subject." }));
        }

        var settingsErrors = ValidateSettings(snapshot);
        if (settingsErrors.Count > 0)
        {
            return TeamRandomPreviewResult.Invalid(BuildInvalidRandomPreviewResponse(subjectId, snapshot, students.Count, settingsErrors));
        }

        var sizing = TryBuildRandomTeamSizes(students.Count, snapshot);
        if (sizing.Errors.Count > 0)
        {
            return TeamRandomPreviewResult.Invalid(BuildInvalidRandomPreviewResponse(subjectId, snapshot, students.Count, sizing.Errors));
        }

        return TeamRandomPreviewResult.Success(new TeamRandomPreviewResponse
        {
            SubjectId = subjectId,
            IsValid = true,
            Teams = BuildRandomPreviewTeams(students, sizing.TeamSizes),
            Errors = Array.Empty<string>(),
            Warnings = Array.Empty<string>(),
            SuggestedParameters = null
        });
    }

    public async Task<TeamValidationResult> ValidateManualDistributionAsync(Guid currentUserId, Guid subjectId, ManualTeamDistributionRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamValidationResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && !CanValidateDistributionInMode(settings.DistributionMode))
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

        if (settings is not null && !CanManageManualDistributionInMode(settings.DistributionMode))
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

        var memberIds = request.Teams.SelectMany(team => team.MemberIds).ToList();
        var usernames = await LoadUsernamesAsync(memberIds, cancellationToken);
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
                MemberIds = teamRequest.MemberIds.ToList(),
                Members = teamRequest.MemberIds
                    .Select(memberId => MapMember(memberId, usernames))
                    .ToList()
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

        if (settings is not null && !CanManageManualDistributionInMode(settings.DistributionMode))
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
        var resultUsernames = await LoadUsernamesAsync(
            resultTeams.SelectMany(team => team.Members.Select(member => member.UserId)),
            cancellationToken);

        return TeamMutationResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(team => MapTeam(team, resultUsernames)).ToList(),
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

        if (settings is not null && !CanManageManualDistributionInMode(settings.DistributionMode))
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
        var resultUsernames = await LoadUsernamesAsync(
            resultTeams.SelectMany(team => team.Members.Select(member => member.UserId)),
            cancellationToken);

        return TeamMutationResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(team => MapTeam(team, resultUsernames)).ToList(),
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

        if (settings.RequiresCaptain)
        {
            if (settings.CaptainSelectionMode is null)
            {
                errors.Add("CaptainSelectionMode must be set when RequiresCaptain is true.");
            }
            else
            {
                if (settings.DistributionMode is TeamDistributionMode.Manual or TeamDistributionMode.Random
                    && settings.CaptainSelectionMode != CaptainSelectionMethod.Voting)
                {
                    errors.Add("CaptainSelectionMode must be Voting for Manual and Random distribution modes.");
                }
            }

            if (settings.CaptainVotingDeadlineDays.HasValue && settings.CaptainVotingDeadlineDays.Value <= 0)
            {
                errors.Add("CaptainVotingDeadlineDays must be greater than zero.");
            }
        }
        else if (settings.DistributionMode == TeamDistributionMode.Draft)
        {
            errors.Add("RequiresCaptain must be true for Draft distribution mode.");
        }

        if (settings.RequiresDecision)
        {
            if (settings.DecisionMode is null)
            {
                errors.Add("DecisionMode must be set when RequiresDecision is true.");
            }
            else if (settings.DecisionMode == SubmissionDecisionMode.CaptainDecides && !settings.RequiresCaptain)
            {
                errors.Add("DecisionMode CaptainDecides requires RequiresCaptain to be true.");
            }

            if (settings.DecisionDeadlineDays.HasValue && settings.DecisionDeadlineDays.Value <= 0)
            {
                errors.Add("DecisionDeadlineDays must be greater than zero.");
            }

            if (settings.RequiredDecisionVotes.HasValue && settings.RequiredDecisionVotes.Value <= 0)
            {
                errors.Add("RequiredDecisionVotes must be greater than zero.");
            }

            if (settings.DecisionMode == SubmissionDecisionMode.CaptainDecides
                && settings.RequiredDecisionVotes.HasValue
                && settings.RequiredDecisionVotes.Value != 1)
            {
                errors.Add("RequiredDecisionVotes must be 1 for CaptainDecides mode.");
            }
        }
        else if (settings.RequiredDecisionVotes.HasValue)
        {
            errors.Add("RequiredDecisionVotes can be set only when RequiresDecision is true.");
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

    private static TeamRandomPreviewResponse BuildInvalidRandomPreviewResponse(
        Guid subjectId,
        SettingsSnapshot settings,
        int totalStudents,
        IReadOnlyList<string> errors)
    {
        IReadOnlyList<string> warnings = totalStudents > 0
            ? GetFeasibilityWarnings(totalStudents, settings)
                .Distinct()
                .ToList()
            : Array.Empty<string>();

        return new TeamRandomPreviewResponse
        {
            SubjectId = subjectId,
            IsValid = false,
            Teams = Array.Empty<RandomTeamPreviewTeamResponse>(),
            Errors = errors,
            Warnings = warnings,
            SuggestedParameters = totalStudents > 0
                ? BuildRandomSuggestion(totalStudents, settings)
                : null
        };
    }

    private static RandomSizingOutcome TryBuildRandomTeamSizes(int totalStudents, SettingsSnapshot settings)
    {
        var outcome = new RandomSizingOutcome();

        if (settings.FixedTeamSize.HasValue)
        {
            var fixedTeamSize = settings.FixedTeamSize.Value;

            if (!IsWithinRange(fixedTeamSize, settings))
            {
                outcome.Errors.Add("FixedTeamSize must be within MinTeamSize and MaxTeamSize.");
                return outcome;
            }

            if (totalStudents % fixedTeamSize != 0)
            {
                outcome.Errors.Add("Total number of students must be divisible by FixedTeamSize.");
                return outcome;
            }

            var teamsCountFromSize = totalStudents / fixedTeamSize;

            if (settings.FixedTeamsCount.HasValue && settings.FixedTeamsCount.Value != teamsCountFromSize)
            {
                outcome.Errors.Add("Total number of students must be equal to FixedTeamsCount multiplied by FixedTeamSize.");
                return outcome;
            }

            if (settings.FixedTeamsCount.HasValue && settings.FixedTeamsCount.Value > totalStudents)
            {
                outcome.Errors.Add("FixedTeamsCount exceeds total number of students.");
                return outcome;
            }

            outcome.TeamSizes.AddRange(Enumerable.Repeat(fixedTeamSize, teamsCountFromSize));
            return outcome;
        }

        if (settings.FixedTeamsCount.HasValue)
        {
            var fixedTeamsCount = settings.FixedTeamsCount.Value;

            if (fixedTeamsCount > totalStudents)
            {
                outcome.Errors.Add("FixedTeamsCount exceeds total number of students.");
                return outcome;
            }

            var sizesForFixedCount = BuildBalancedTeamSizes(totalStudents, fixedTeamsCount);

            if (sizesForFixedCount.Any(size => !IsWithinRange(size, settings)))
            {
                outcome.Errors.Add("Total number of students does not fit FixedTeamsCount with MinTeamSize and MaxTeamSize.");
                return outcome;
            }

            outcome.TeamSizes.AddRange(sizesForFixedCount);
            return outcome;
        }

        var candidateCounts = GetBalancedTeamCounts(totalStudents, settings);
        if (candidateCounts.Count == 0)
        {
            outcome.Errors.Add("Current number of students cannot be distributed within MinTeamSize and MaxTeamSize.");
            return outcome;
        }

        var preferredCount = ChoosePreferredTeamCount(totalStudents, settings, candidateCounts);
        outcome.TeamSizes.AddRange(BuildBalancedTeamSizes(totalStudents, preferredCount));

        return outcome;
    }

    private static IReadOnlyList<RandomTeamPreviewTeamResponse> BuildRandomPreviewTeams(
        IReadOnlyList<Guid> studentIds,
        IReadOnlyList<int> teamSizes)
    {
        var shuffledStudents = studentIds.ToList();
        Shuffle(shuffledStudents);

        var teams = new List<RandomTeamPreviewTeamResponse>(teamSizes.Count);
        var index = 0;

        foreach (var teamSize in teamSizes)
        {
            teams.Add(new RandomTeamPreviewTeamResponse
            {
                MemberIds = shuffledStudents
                    .Skip(index)
                    .Take(teamSize)
                    .ToList()
            });

            index += teamSize;
        }

        return teams;
    }

    private static List<int> BuildBalancedTeamSizes(int totalStudents, int teamsCount)
    {
        var baseSize = totalStudents / teamsCount;
        var remainder = totalStudents % teamsCount;
        var teamSizes = new List<int>(teamsCount);

        for (var index = 0; index < teamsCount; index++)
        {
            teamSizes.Add(index < remainder ? baseSize + 1 : baseSize);
        }

        return teamSizes;
    }

    private static RandomTeamDistributionSuggestionResponse BuildRandomSuggestion(int totalStudents, SettingsSnapshot settings)
    {
        var boundsOnlySettings = new SettingsSnapshot(
            TeamDistributionMode.Random,
            null,
            null,
            settings.MinTeamSize,
            settings.MaxTeamSize,
            null,
            null,
            false,
            null,
            null,
            null,
            false);

        var candidateCounts = GetBalancedTeamCounts(totalStudents, boundsOnlySettings);

        if (candidateCounts.Count == 0)
        {
            candidateCounts = Enumerable.Range(1, totalStudents).ToList();
        }

        var preferredCount = settings.FixedTeamsCount.HasValue && settings.FixedTeamsCount.Value > 0
            ? Math.Clamp(settings.FixedTeamsCount.Value, 1, totalStudents)
            : ChoosePreferredTeamCount(totalStudents, settings, candidateCounts);

        var chosenCount = candidateCounts
            .OrderBy(count => Math.Abs(count - preferredCount))
            .ThenBy(count => totalStudents % count == 0 ? 0 : 1)
            .ThenBy(count => count)
            .First();

        var teamSizes = BuildBalancedTeamSizes(totalStudents, chosenCount);
        var minTeamSize = teamSizes.Min();
        var maxTeamSize = teamSizes.Max();

        return new RandomTeamDistributionSuggestionResponse
        {
            SuggestedTeamsCount = chosenCount,
            SuggestedMinTeamSize = minTeamSize,
            SuggestedMaxTeamSize = maxTeamSize,
            SuggestedFixedTeamSize = minTeamSize == maxTeamSize ? minTeamSize : null,
            SuggestedTeamSizes = teamSizes
        };
    }

    private static List<int> GetBalancedTeamCounts(int totalStudents, SettingsSnapshot settings)
    {
        var counts = new List<int>();

        for (var teamsCount = 1; teamsCount <= totalStudents; teamsCount++)
        {
            var minSize = totalStudents / teamsCount;
            var maxSize = minSize + (totalStudents % teamsCount == 0 ? 0 : 1);

            if (!IsWithinRange(minSize, settings) || !IsWithinRange(maxSize, settings))
            {
                continue;
            }

            counts.Add(teamsCount);
        }

        return counts;
    }

    private static int ChoosePreferredTeamCount(int totalStudents, SettingsSnapshot settings, IReadOnlyList<int> candidateCounts)
    {
        var targetTeamSize = GetTargetTeamSize(totalStudents, settings);
        var targetCount = Math.Clamp((int)Math.Round(totalStudents / (double)targetTeamSize), 1, totalStudents);

        return candidateCounts
            .OrderBy(count => totalStudents % count == 0 ? 0 : 1)
            .ThenBy(count => Math.Abs(count - targetCount))
            .ThenBy(count => Math.Abs((totalStudents / (double)count) - targetTeamSize))
            .ThenBy(count => count)
            .First();
    }

    private static int GetTargetTeamSize(int totalStudents, SettingsSnapshot settings)
    {
        if (settings.FixedTeamSize.HasValue && settings.FixedTeamSize.Value > 0)
        {
            return settings.FixedTeamSize.Value;
        }

        if (settings.MinTeamSize.HasValue && settings.MaxTeamSize.HasValue
            && settings.MinTeamSize.Value > 0
            && settings.MaxTeamSize.Value > 0)
        {
            return Math.Max(1, (int)Math.Round((settings.MinTeamSize.Value + settings.MaxTeamSize.Value) / 2d));
        }

        if (settings.MinTeamSize.HasValue && settings.MinTeamSize.Value > 0)
        {
            return settings.MinTeamSize.Value;
        }

        if (settings.MaxTeamSize.HasValue && settings.MaxTeamSize.Value > 0)
        {
            return settings.MaxTeamSize.Value;
        }

        return Math.Max(2, (int)Math.Round(Math.Sqrt(totalStudents)));
    }

    private static bool IsWithinRange(int teamSize, SettingsSnapshot settings)
    {
        var minTeamSize = settings.MinTeamSize ?? 1;
        var maxTeamSize = settings.MaxTeamSize ?? int.MaxValue;
        return teamSize >= minTeamSize && teamSize <= maxTeamSize;
    }

    private static void Shuffle(List<Guid> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static bool CanManageManualDistributionInMode(TeamDistributionMode mode)
    {
        return mode is TeamDistributionMode.Manual or TeamDistributionMode.Random;
    }

    private static bool CanValidateDistributionInMode(TeamDistributionMode mode)
    {
        return mode is TeamDistributionMode.Manual or TeamDistributionMode.Random or TeamDistributionMode.Students;
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
            CaptainSelectionMode = settings.CaptainSelectionMode,
            CaptainVotingDeadlineDays = settings.CaptainVotingDeadlineDays,
            RequiresCaptain = settings.RequiresCaptain,
            DecisionMode = settings.DecisionMode,
            DecisionDeadlineDays = settings.DecisionDeadlineDays,
            RequiredDecisionVotes = settings.RequiredDecisionVotes,
            RequiresDecision = settings.RequiresDecision,
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

    private async Task<Dictionary<Guid, string>> LoadUsernamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _dbContext.Users
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.UserName })
            .ToDictionaryAsync(x => x.Id, x => x.UserName ?? string.Empty, cancellationToken);
    }

    private static TeamResponse MapTeam(Team team, IReadOnlyDictionary<Guid, string> usernames)
    {
        // Captain can be identified either by TeamMember.IsCaptain (Draft mode)
        // or by Team.CaptainUserId (Voting/Random/Manual selection)
        var captainFromMember = team.Members.FirstOrDefault(m => m.IsCaptain);
        var captainId = captainFromMember?.UserId ?? team.CaptainUserId;

        return new TeamResponse
        {
            Id = team.Id,
            SubjectId = team.SubjectId,
            Name = team.Name,
            CaptainId = captainId,
            MemberIds = team.Members.Select(member => member.UserId).ToList(),
            Members = team.Members.Select(member => MapMember(member, usernames, team.CaptainUserId)).ToList()
        };
    }

    private static TeamMemberResponse MapMember(TeamMember member, IReadOnlyDictionary<Guid, string> usernames, Guid? teamCaptainUserId = null)
    {
        // A member is captain if either IsCaptain flag is set (Draft mode)
        // or if they match the Team.CaptainUserId (Voting/Random/Manual selection)
        var isCaptain = member.IsCaptain || member.UserId == teamCaptainUserId;
        return new TeamMemberResponse
        {
            UserId = member.UserId,
            Username = usernames.TryGetValue(member.UserId, out var username) ? username : string.Empty,
            IsCaptain = isCaptain
        };
    }

    private static TeamMemberResponse MapMember(Guid userId, IReadOnlyDictionary<Guid, string> usernames)
    {
        return new TeamMemberResponse
        {
            UserId = userId,
            Username = usernames.TryGetValue(userId, out var username) ? username : string.Empty,
            IsCaptain = false
        };
    }

    public async Task<DraftStartResult> StartDraftAsync(Guid currentUserId, Guid subjectId, DraftStartRequest request, CancellationToken cancellationToken)
    {
        if (!await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken))
        {
            return DraftStartResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is not null && settings.DistributionMode != TeamDistributionMode.Draft)
        {
            return DraftStartResult.Invalid(new[] { "Distribution mode must be Draft." });
        }

        if (settings is not null && settings.IsFinalized)
        {
            return DraftStartResult.Invalid(new[] { "Teams are already finalized." });
        }

        var existingDraft = await _dbContext.DraftStates
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (existingDraft is not null && existingDraft.IsActive)
        {
            return DraftStartResult.Invalid(new[] { "A draft is already in progress." });
        }

        var errors = new List<string>();

        if (request.CaptainIds.Count == 0)
        {
            errors.Add("At least one captain must be specified.");
            return DraftStartResult.Invalid(errors);
        }

        if (request.CaptainIds.Distinct().Count() != request.CaptainIds.Count)
        {
            errors.Add("Captain IDs must be unique.");
        }

        var studentIds = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var nonStudentCaptains = request.CaptainIds.Except(studentIds).ToList();
        if (nonStudentCaptains.Count > 0)
        {
            errors.Add("All captains must be students of the subject.");
        }

        var snapshot = SettingsSnapshot.From(settings);
        var teamsCount = request.CaptainIds.Count;

        if (snapshot.FixedTeamsCount.HasValue && snapshot.FixedTeamsCount.Value != teamsCount)
        {
            errors.Add($"Number of captains ({teamsCount}) must match FixedTeamsCount ({snapshot.FixedTeamsCount.Value}).");
        }

        var nonCaptainStudents = studentIds.Except(request.CaptainIds).ToList();
        var studentsPerTeam = teamsCount > 0 ? (nonCaptainStudents.Count + teamsCount) / teamsCount : 0;

        if (snapshot.MaxTeamSize.HasValue && studentsPerTeam > snapshot.MaxTeamSize.Value && teamsCount > 0)
        {
            var minTeamsNeeded = (int)Math.Ceiling((double)(studentIds.Count) / snapshot.MaxTeamSize.Value);
            if (teamsCount < minTeamsNeeded)
            {
                errors.Add($"Not enough captains to fit all students within MaxTeamSize ({snapshot.MaxTeamSize.Value}). At least {minTeamsNeeded} captains needed.");
            }
        }

        if (errors.Count > 0)
        {
            return DraftStartResult.Invalid(errors);
        }

        var existingTeams = await _dbContext.Teams
            .Where(x => x.SubjectId == subjectId)
            .ToListAsync(cancellationToken);
        if (existingTeams.Count > 0)
        {
            _dbContext.Teams.RemoveRange(existingTeams);
        }

        var subject = await _dbContext.Subjects.SingleAsync(x => x.Id == subjectId, cancellationToken);
        var captainOrder = request.CaptainIds.ToList();

        foreach (var captainId in captainOrder)
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                CreatedAt = _timeProvider.GetUtcNow(),
                Subject = subject
            };

            team.Members = new List<TeamMember>
            {
                new TeamMember
                {
                    Id = Guid.NewGuid(),
                    TeamId = team.Id,
                    UserId = captainId,
                    IsCaptain = true,
                    Team = team
                }
            };

            _dbContext.Teams.Add(team);
        }

        var settingsEntity = settings ?? new SubjectTeamSettings
        {
            SubjectId = subjectId,
            DistributionMode = TeamDistributionMode.Draft,
            IsFinalized = false
        };
        settingsEntity.DistributionMode = TeamDistributionMode.Draft;
        settingsEntity.IsFinalized = false;
        settingsEntity.FinalizedAt = null;
        if (settings is null)
        {
            _dbContext.SubjectTeamSettings.Add(settingsEntity);
        }

        if (existingDraft is not null)
        {
            existingDraft.IsActive = true;
            existingDraft.IsCompleted = false;
            existingDraft.CurrentCaptainIndex = 0;
            existingDraft.CurrentRound = 1;
            existingDraft.CaptainOrder = JsonSerializer.Serialize(captainOrder);
            existingDraft.StartedAt = _timeProvider.GetUtcNow();
            existingDraft.CompletedAt = null;
        }
        else
        {
            _dbContext.DraftStates.Add(new DraftState
            {
                SubjectId = subjectId,
                IsActive = true,
                IsCompleted = false,
                CurrentCaptainIndex = 0,
                CurrentRound = 1,
                CaptainOrder = JsonSerializer.Serialize(captainOrder),
                StartedAt = _timeProvider.GetUtcNow(),
                CompletedAt = null,
                Subject = subject
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return DraftStartResult.Success(await BuildDraftStateResponseAsync(subjectId, cancellationToken));
    }

    public async Task<DraftStateResult> GetDraftStateAsync(Guid currentUserId, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return DraftStateResult.Forbidden();
        }

        var draftState = await _dbContext.DraftStates
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (draftState is null)
        {
            return DraftStateResult.NotFound();
        }

        return DraftStateResult.Success(await BuildDraftStateResponseAsync(subjectId, cancellationToken));
    }

    public async Task<DraftPickResult> DraftPickAsync(Guid currentUserId, Guid subjectId, DraftPickRequest request, CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, subjectId, cancellationToken))
        {
            return DraftPickResult.Forbidden();
        }

        var draftState = await _dbContext.DraftStates
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (draftState is null || !draftState.IsActive)
        {
            return DraftPickResult.Invalid(new[] { "No active draft found." });
        }

        if (draftState.IsCompleted)
        {
            return DraftPickResult.Invalid(new[] { "Draft is already completed." });
        }

        var captainOrder = JsonSerializer.Deserialize<List<Guid>>(draftState.CaptainOrder) ?? new List<Guid>();
        if (captainOrder.Count == 0)
        {
            return DraftPickResult.Invalid(new[] { "Draft captain order is corrupted." });
        }

        var currentCaptainId = captainOrder[draftState.CurrentCaptainIndex];

        var isTeacher = await IsTeacherOrAdminAsync(currentUserId, subjectId, cancellationToken);
        if (currentUserId != currentCaptainId && !isTeacher)
        {
            return DraftPickResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);
        var snapshot = SettingsSnapshot.From(settings);

        var studentId = request.StudentId;
        var isStudent = await _dbContext.SubjectParticipants
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == studentId && x.Role == StudentRole, cancellationToken);

        if (!isStudent)
        {
            return DraftPickResult.Invalid(new[] { "Selected user is not a student of this subject." });
        }

        var isAlreadyAssigned = await _dbContext.TeamMembers
            .AnyAsync(x => x.Team.SubjectId == subjectId && x.UserId == studentId, cancellationToken);

        if (isAlreadyAssigned)
        {
            return DraftPickResult.Invalid(new[] { "Student is already assigned to a team." });
        }

        var captainTeam = await _dbContext.Teams
            .Include(x => x.Members)
            .Where(x => x.SubjectId == subjectId && x.Members.Any(m => m.UserId == currentCaptainId && m.IsCaptain))
            .SingleOrDefaultAsync(cancellationToken);

        if (captainTeam is null)
        {
            return DraftPickResult.Invalid(new[] { "Captain's team not found." });
        }

        var maxTeamSize = snapshot.FixedTeamSize ?? snapshot.MaxTeamSize;
        if (maxTeamSize.HasValue && captainTeam.Members.Count >= maxTeamSize.Value)
        {
            return DraftPickResult.Invalid(new[] { $"Team has reached maximum size ({maxTeamSize.Value})." });
        }

        captainTeam.Members.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = captainTeam.Id,
            UserId = studentId,
            IsCaptain = false,
            Team = captainTeam
        });

        // Advance to next captain (round-robin)
        await AdvanceDraftTurnAsync(draftState, subjectId, snapshot, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return DraftPickResult.Success(await BuildDraftStateResponseAsync(subjectId, cancellationToken));
    }

    private async Task AdvanceDraftTurnAsync(DraftState draftState, Guid subjectId, SettingsSnapshot snapshot, CancellationToken cancellationToken)
    {
        var captainOrder = JsonSerializer.Deserialize<List<Guid>>(draftState.CaptainOrder) ?? new List<Guid>();
        var totalCaptains = captainOrder.Count;

        var allStudentIds = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var assignedIds = await _dbContext.TeamMembers
            .Where(x => x.Team.SubjectId == subjectId)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var availableStudents = allStudentIds.Except(assignedIds).ToList();

        if (availableStudents.Count == 0)
        {
            draftState.IsActive = false;
            draftState.IsCompleted = true;
            draftState.CompletedAt = _timeProvider.GetUtcNow();
            return;
        }

        var teams = await _dbContext.Teams
            .Include(x => x.Members)
            .Where(x => x.SubjectId == subjectId)
            .ToListAsync(cancellationToken);

        var maxTeamSize = snapshot.FixedTeamSize ?? snapshot.MaxTeamSize;

        var nextIndex = (draftState.CurrentCaptainIndex + 1) % totalCaptains;
        var newRound = nextIndex <= draftState.CurrentCaptainIndex ? draftState.CurrentRound + 1 : draftState.CurrentRound;
        var checked1 = 0;

        while (checked1 < totalCaptains)
        {
            var candidateCaptainId = captainOrder[nextIndex];
            var candidateTeam = teams.FirstOrDefault(t => t.Members.Any(m => m.UserId == candidateCaptainId && m.IsCaptain));

            if (candidateTeam is not null && (!maxTeamSize.HasValue || candidateTeam.Members.Count < maxTeamSize.Value))
            {
                draftState.CurrentCaptainIndex = nextIndex;
                draftState.CurrentRound = newRound;
                return;
            }

            nextIndex = (nextIndex + 1) % totalCaptains;
            if (nextIndex == 0) newRound++;
            checked1++;
        }

        draftState.IsActive = false;
        draftState.IsCompleted = true;
        draftState.CompletedAt = _timeProvider.GetUtcNow();
    }

    private async Task<DraftStateResponse> BuildDraftStateResponseAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        var draftState = await _dbContext.DraftStates
            .SingleAsync(x => x.SubjectId == subjectId, cancellationToken);

        var captainOrder = JsonSerializer.Deserialize<List<Guid>>(draftState.CaptainOrder) ?? new List<Guid>();
        Guid? currentCaptainId = draftState.IsActive && !draftState.IsCompleted && captainOrder.Count > 0
            ? captainOrder[draftState.CurrentCaptainIndex]
            : null;

        var teams = await LoadTeamsAsync(subjectId, cancellationToken);
        var usernames = await LoadUsernamesAsync(
            teams.SelectMany(t => t.Members.Select(m => m.UserId)),
            cancellationToken);

        var allStudentIds = await _dbContext.SubjectParticipants
            .Where(x => x.SubjectId == subjectId && x.Role == StudentRole)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);

        var assignedIds = teams.SelectMany(t => t.Members.Select(m => m.UserId)).ToHashSet();
        var availableIds = allStudentIds.Except(assignedIds).ToList();

        var availableUsernames = await LoadUsernamesAsync(availableIds, cancellationToken);

        return new DraftStateResponse
        {
            SubjectId = subjectId,
            IsActive = draftState.IsActive,
            IsCompleted = draftState.IsCompleted,
            CurrentCaptainId = currentCaptainId,
            CurrentRound = draftState.CurrentRound,
            Teams = teams.Select(t => MapTeam(t, usernames)).ToList(),
            AvailableStudents = availableIds
                .Select(id => new TeamMemberResponse
                {
                    UserId = id,
                    Username = availableUsernames.TryGetValue(id, out var name) ? name : string.Empty,
                    IsCaptain = false
                })
                .ToList()
        };
    }
    public async Task<TeamMutationResult> StudentCreateTeamAsync(Guid currentUserId, Guid subjectId, StudentCreateTeamRequest request, CancellationToken cancellationToken)
    {
        if (!await IsStudentAsync(currentUserId, subjectId, cancellationToken))
        {
            return TeamMutationResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is null || settings.DistributionMode != TeamDistributionMode.Students)
        {
            return TeamMutationResult.Invalid(new[] { "Distribution mode must be Students." });
        }

        if (settings.IsFinalized)
        {
            return TeamMutationResult.Invalid(new[] { "Teams are already finalized." });
        }

        var alreadyInTeam = await _dbContext.TeamMembers
            .AnyAsync(x => x.Team.SubjectId == subjectId && x.UserId == currentUserId, cancellationToken);

        if (alreadyInTeam)
        {
            return TeamMutationResult.Invalid(new[] { "You are already a member of a team." });
        }

        var snapshot = SettingsSnapshot.From(settings);
        if (snapshot.FixedTeamsCount.HasValue)
        {
            var currentTeamsCount = await _dbContext.Teams
                .CountAsync(x => x.SubjectId == subjectId, cancellationToken);

            if (currentTeamsCount >= snapshot.FixedTeamsCount.Value)
            {
                return TeamMutationResult.Invalid(new[] { $"Maximum number of teams ({snapshot.FixedTeamsCount.Value}) has been reached." });
            }
        }

        var subject = await _dbContext.Subjects.SingleAsync(x => x.Id == subjectId, cancellationToken);

        var team = new Team
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            Name = request.Name,
            CreatedAt = _timeProvider.GetUtcNow(),
            Subject = subject
        };

        team.Members = new List<TeamMember>
        {
            new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = currentUserId,
                IsCaptain = settings.RequiresCaptain,
                Team = team
            }
        };

        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var resultTeams = await LoadTeamsAsync(subjectId, cancellationToken);
        var resultUsernames = await LoadUsernamesAsync(
            resultTeams.SelectMany(t => t.Members.Select(m => m.UserId)),
            cancellationToken);

        return TeamMutationResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(t => MapTeam(t, resultUsernames)).ToList(),
            Warnings = Array.Empty<string>()
        });
    }

    public async Task<StudentJoinTeamResult> StudentJoinTeamAsync(Guid currentUserId, Guid subjectId, Guid teamId, CancellationToken cancellationToken)
    {
        if (!await IsStudentAsync(currentUserId, subjectId, cancellationToken))
        {
            return StudentJoinTeamResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is null || settings.DistributionMode != TeamDistributionMode.Students)
        {
            return StudentJoinTeamResult.Invalid(new[] { "Distribution mode must be Students." });
        }

        if (settings.IsFinalized)
        {
            return StudentJoinTeamResult.Invalid(new[] { "Teams are already finalized." });
        }

        // Check if student is already in a team
        var alreadyInTeam = await _dbContext.TeamMembers
            .AnyAsync(x => x.Team.SubjectId == subjectId && x.UserId == currentUserId, cancellationToken);

        if (alreadyInTeam)
        {
            return StudentJoinTeamResult.Invalid(new[] { "You are already a member of a team." });
        }

        var team = await _dbContext.Teams
            .Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Id == teamId && x.SubjectId == subjectId, cancellationToken);

        if (team is null)
        {
            return StudentJoinTeamResult.Invalid(new[] { "Team not found." });
        }

        var snapshot = SettingsSnapshot.From(settings);
        var maxSize = snapshot.FixedTeamSize ?? snapshot.MaxTeamSize;

        if (maxSize.HasValue && team.Members.Count >= maxSize.Value)
        {
            return StudentJoinTeamResult.Invalid(new[] { $"Team has reached maximum size ({maxSize.Value})." });
        }

        team.Members.Add(new TeamMember
        {
            Id = Guid.NewGuid(),
            TeamId = team.Id,
            UserId = currentUserId,
            IsCaptain = false,
            Team = team
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var resultTeams = await LoadTeamsAsync(subjectId, cancellationToken);
        var resultUsernames = await LoadUsernamesAsync(
            resultTeams.SelectMany(t => t.Members.Select(m => m.UserId)),
            cancellationToken);

        return StudentJoinTeamResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(t => MapTeam(t, resultUsernames)).ToList(),
            Warnings = Array.Empty<string>()
        });
    }

    public async Task<StudentLeaveTeamResult> StudentLeaveTeamAsync(Guid currentUserId, Guid subjectId, Guid teamId, CancellationToken cancellationToken)
    {
        if (!await IsStudentAsync(currentUserId, subjectId, cancellationToken))
        {
            return StudentLeaveTeamResult.Forbidden();
        }

        var settings = await _dbContext.SubjectTeamSettings
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (settings is null || settings.DistributionMode != TeamDistributionMode.Students)
        {
            return StudentLeaveTeamResult.Invalid(new[] { "Distribution mode must be Students." });
        }

        if (settings.IsFinalized)
        {
            return StudentLeaveTeamResult.Invalid(new[] { "Teams are already finalized." });
        }

        var team = await _dbContext.Teams
            .Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Id == teamId && x.SubjectId == subjectId, cancellationToken);

        if (team is null)
        {
            return StudentLeaveTeamResult.Invalid(new[] { "Team not found." });
        }

        var member = team.Members.SingleOrDefault(m => m.UserId == currentUserId);
        if (member is null)
        {
            return StudentLeaveTeamResult.Invalid(new[] { "You are not a member of this team." });
        }

        if (member.IsCaptain)
        {
            // Captain can only leave if they are the only member
            if (team.Members.Count > 1)
            {
                return StudentLeaveTeamResult.Invalid(new[] { "Captain cannot leave the team while other members are present." });
            }

            // Captain is alone — delete the whole team
            _dbContext.Teams.Remove(team);
        }
        else
        {
            _dbContext.TeamMembers.Remove(member);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var resultTeams = await LoadTeamsAsync(subjectId, cancellationToken);
        var resultUsernames = await LoadUsernamesAsync(
            resultTeams.SelectMany(t => t.Members.Select(m => m.UserId)),
            cancellationToken);

        return StudentLeaveTeamResult.Success(new TeamDistributionResponse
        {
            Teams = resultTeams.Select(t => MapTeam(t, resultUsernames)).ToList(),
            Warnings = Array.Empty<string>()
        });
    }

    private async Task<bool> IsStudentAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        return await _dbContext.SubjectParticipants
            .AnyAsync(
                x => x.SubjectId == subjectId
                     && x.UserId == userId
                     && x.Role == StudentRole,
                cancellationToken);
    }

    private sealed class ValidationOutcome
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
    }

    private sealed class RandomSizingOutcome
    {
        public List<int> TeamSizes { get; } = new();
        public List<string> Errors { get; } = new();
    }

    private readonly record struct SettingsSnapshot(
        TeamDistributionMode DistributionMode,
        int? FixedTeamsCount,
        int? FixedTeamSize,
        int? MinTeamSize,
        int? MaxTeamSize,
        CaptainSelectionMethod? CaptainSelectionMode,
        int? CaptainVotingDeadlineDays,
        bool RequiresCaptain,
        SubmissionDecisionMode? DecisionMode,
        int? DecisionDeadlineDays,
        int? RequiredDecisionVotes,
        bool RequiresDecision)
    {
        public static SettingsSnapshot From(SubjectTeamSettings? settings)
        {
            if (settings is null)
            {
                return new SettingsSnapshot(TeamDistributionMode.Manual, null, null, null, null, null, null, false, null, null, null, false);
            }

            return new SettingsSnapshot(
                settings.DistributionMode,
                settings.FixedTeamsCount,
                settings.FixedTeamSize,
                settings.MinTeamSize,
                settings.MaxTeamSize,
                settings.CaptainSelectionMode,
                settings.CaptainVotingDeadlineDays,
                settings.RequiresCaptain,
                settings.DecisionMode,
                settings.DecisionDeadlineDays,
                settings.RequiredDecisionVotes,
                settings.RequiresDecision);
        }
    }
}
