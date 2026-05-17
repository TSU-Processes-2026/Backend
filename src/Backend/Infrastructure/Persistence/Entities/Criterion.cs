namespace Infrastructure.Persistence.Entities;

public sealed class Criterion
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public required string Description { get; set; }
    public required string Format { get; set; }
    public decimal? Weight { get; set; }
    public decimal? MaxPoints { get; set; }
    public decimal? Points { get; set; }
    public bool IsBonus { get; set; }
    public bool IsPenalty { get; set; }
    public int Order { get; set; }
    public Post Task { get; set; } = null!;
}
