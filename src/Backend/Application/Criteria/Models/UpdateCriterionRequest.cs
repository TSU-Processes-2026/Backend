namespace Application.Criteria.Models;

public sealed class UpdateCriterionRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? CriterionType { get; init; }
    public string? ValueType { get; init; }
    public string? Format { get; init; }
    public decimal? Weight { get; init; }
    public decimal? MinValue { get; init; }
    public decimal? MaxPoints { get; init; }
    public decimal? Points { get; init; }
    public bool? IsBonus { get; init; }
    public bool? IsPenalty { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsHiddenUntilVisibility { get; init; }
    public string? AppliesTo { get; init; }
    public int? Order { get; init; }
}
