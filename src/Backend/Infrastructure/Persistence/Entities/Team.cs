using Application.Teams.Models;
using Infrastructure.Identity;

namespace Infrastructure.Persistence.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CaptainUserId { get; set; }
    public CaptainSelectionMethod? SelectionMethod { get; set; }
    public DateTimeOffset? CaptainSelectedAt { get; set; }

    public Subject Subject { get; set; } = null!;
    public ApplicationUser? Captain { get; set; }
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<CaptainVotingSession> CaptainVotingSessions { get; set; } = new List<CaptainVotingSession>();
    public ICollection<TeamGrade> TeamGrades { get; set; } = new List<TeamGrade>();
}
