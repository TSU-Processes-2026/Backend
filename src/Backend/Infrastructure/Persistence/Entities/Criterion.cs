namespace Infrastructure.Persistence.Entities;

public sealed class Criterion
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string CriterionType { get; set; }
    public required string Format { get; set; }
    public decimal? Weight { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxPoints { get; set; }
    public decimal? Points { get; set; }
    public bool IsBonus { get; set; }
    public bool IsPenalty { get; set; }
    public bool IsRequired { get; set; }
    public bool IsHiddenUntilVisibility { get; set; }
    public required string AppliesTo { get; set; }
    public int Order { get; set; }
    public Post Task { get; set; } = null!;
}
