namespace Application.Criteria.Models;

public sealed class UpsertCriterionResultsRequest
{
    public IReadOnlyList<UpsertCriterionResultRequest> Results { get; init; } = Array.Empty<UpsertCriterionResultRequest>();
}
