namespace Application.Criteria.Models;

public sealed class UpsertCriterionResultRequest
{
    public Guid CriterionId { get; init; }
    public decimal Value { get; init; }
    public string? Comment { get; init; }
}
