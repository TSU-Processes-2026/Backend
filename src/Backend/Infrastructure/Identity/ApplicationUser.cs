using Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<AuthSession> Sessions { get; } = new List<AuthSession>();
    public ICollection<SubjectParticipant> SubjectParticipants { get; } = new List<SubjectParticipant>();
}
