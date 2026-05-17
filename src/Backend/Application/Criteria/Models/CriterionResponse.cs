namespace Application.Criteria.Models;

public sealed class CriterionResponse
{
    public required Guid Id { get; init; }
    public required Guid TaskId { get; init; }
    public required string Description { get; init; }
    public required string Format { get; init; }
    public decimal? Weight { get; init; }
    public decimal? MaxPoints { get; init; }
    public decimal? Points { get; init; }
    public bool IsBonus { get; init; }
    public bool IsPenalty { get; init; }
    public required int Order { get; init; }
}
