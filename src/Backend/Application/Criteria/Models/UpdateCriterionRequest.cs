namespace Application.Criteria.Models;

public sealed class UpdateCriterionRequest
{
    public string? Description { get; init; }
    public string? Format { get; init; }
    public decimal? Weight { get; init; }
    public decimal? MaxPoints { get; init; }
    public decimal? Points { get; init; }
    public bool? IsBonus { get; init; }
    public bool? IsPenalty { get; init; }
    public int? Order { get; init; }
}
