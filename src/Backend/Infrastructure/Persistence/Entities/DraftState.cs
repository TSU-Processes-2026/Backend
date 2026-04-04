namespace Infrastructure.Persistence.Entities;

public sealed class DraftState
{
    public Guid SubjectId { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public int CurrentCaptainIndex { get; set; }
    public int CurrentRound { get; set; }
    public string CaptainOrder { get; set; } = "[]";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Subject Subject { get; set; } = null!;
}
