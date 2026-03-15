using Application.Submissions.Models;
using Infrastructure.Identity;
using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class DataSeeder
{
    private const string AdminRoleName = "Admin";
    private const string StudentRoleName = "Student";
    private const string AssignmentPostType = "Assignment";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = provider.GetRequiredService<LmsDbContext>();

        var adminUser = await EnsureUserAsync(userManager, "admin@ad.min", "admin123", cancellationToken);
        await EnsureIdentityRoleAsync(roleManager, AdminRoleName);
        await EnsureUserInRoleAsync(userManager, adminUser, AdminRoleName);

        var studentUser = await EnsureUserAsync(userManager, "student@st.ud", "student123", cancellationToken);

        await EnsureAdminParticipationInExistingSubjectsAsync(db, adminUser.Id, cancellationToken);

        var seededSubject = await EnsureSeededSubjectAsync(db, cancellationToken);
        await EnsureParticipantAsync(db, seededSubject.Id, adminUser.Id, AdminRoleName, cancellationToken);
        await EnsureParticipantAsync(db, seededSubject.Id, studentUser.Id, StudentRoleName, cancellationToken);

        var seededAssignment = await EnsureSeededAssignmentAsync(db, seededSubject.Id, adminUser.Id, cancellationToken);
        await EnsureSeededSubmissionAsync(db, seededAssignment, studentUser.Id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureIdentityRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        });

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create role {roleName}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("D")
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user {email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private static async Task EnsureUserInRoleAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string roleName)
    {
        if (await userManager.IsInRoleAsync(user, roleName))
        {
            return;
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to add user {user.Email} to role {roleName}: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }

    private static async Task EnsureAdminParticipationInExistingSubjectsAsync(LmsDbContext db, Guid adminUserId, CancellationToken cancellationToken)
    {
        var subjectIds = await db.Subjects
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var subjectId in subjectIds)
        {
            await EnsureParticipantAsync(db, subjectId, adminUserId, AdminRoleName, cancellationToken);
        }
    }

    private static async Task<Subject> EnsureSeededSubjectAsync(LmsDbContext db, CancellationToken cancellationToken)
    {
        const string seededSubjectTitle = "Seeded Subject";

        var existingSubject = await db.Subjects
            .Include(x => x.Participants)
            .SingleOrDefaultAsync(x => x.Title == seededSubjectTitle, cancellationToken);

        if (existingSubject is not null)
        {
            return existingSubject;
        }

        var createdSubject = new Subject
        {
            Id = Guid.NewGuid(),
            Title = seededSubjectTitle,
            Description = "Subject created by DataSeeder for quick local testing.",
            Participants = new List<SubjectParticipant>()
        };

        db.Subjects.Add(createdSubject);
        return createdSubject;
    }

    private static async Task<Post> EnsureSeededAssignmentAsync(
        LmsDbContext db,
        Guid subjectId,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        const string seededAssignmentContent = "Seeded Assignment";

        var existingAssignment = await db.Posts
            .Include(x => x.Questions)
            .ThenInclude(x => x.Options)
            .SingleOrDefaultAsync(
                x => x.SubjectId == subjectId
                     && x.PostType == AssignmentPostType
                     && x.Content == seededAssignmentContent,
                cancellationToken);

        if (existingAssignment is not null)
        {
            return existingAssignment;
        }

        var questionId = Guid.NewGuid();

        var assignment = new Post
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            AuthorId = adminUserId,
            PostType = AssignmentPostType,
            Content = seededAssignmentContent,
            CreatedAt = DateTimeOffset.UtcNow,
            AssignmentData = "{\"type\":\"manual\"}",
            Subject = null!,
            Questions = new List<AssignmentQuestion>
            {
                new()
                {
                    Id = questionId,
                    QuestionType = "Text",
                    QuestionData = "Describe the subject in one sentence.",
                    Options = new List<AssignmentQuestionOption>()
                }
            }
        };

        db.Posts.Add(assignment);
        return assignment;
    }

    private static async Task EnsureSeededSubmissionAsync(LmsDbContext db, Post assignment, Guid studentUserId, CancellationToken cancellationToken)
    {
        var existingSubmission = await db.Submissions
            .AsNoTracking()
            .AnyAsync(x => x.assignmentId == assignment.Id && x.authorId == studentUserId, cancellationToken);

        if (existingSubmission)
        {
            return;
        }

        var questionId = assignment.Questions.FirstOrDefault()?.Id ?? Guid.NewGuid();

        db.Submissions.Add(new Submission
        {
            id = Guid.NewGuid(),
            assignmentId = assignment.Id,
            post = assignment,
            authorId = studentUserId,
            answers = new List<AnswerItem>
            {
                new()
                {
                    id = Guid.NewGuid(),
                    assignmentQuestionId = questionId,
                    answerType = AnswerTypeEnum.TextAnswer,
                    text = "Seeded student submission answer."
                }
            },
            status = SubmissionStatusEnum.Draft,
            submittedAt = DateTime.UtcNow
        });
    }

    private static async Task EnsureParticipantAsync(
        LmsDbContext db,
        Guid subjectId,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var participant = await db.SubjectParticipants
            .SingleOrDefaultAsync(x => x.SubjectId == subjectId && x.UserId == userId, cancellationToken);

        if (participant is null)
        {
            db.SubjectParticipants.Add(new SubjectParticipant
            {
                SubjectId = subjectId,
                UserId = userId,
                Role = role,
                Subject = null!
            });

            return;
        }

        if (!string.Equals(participant.Role, role, StringComparison.Ordinal))
        {
            participant.Role = role;
        }
    }
}
