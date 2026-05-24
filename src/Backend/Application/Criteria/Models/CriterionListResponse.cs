namespace Application.Criteria.Models;

public sealed class CriterionListResponse
{
    public required bool Hidden { get; init; }
    public required IReadOnlyList<CriterionResponse> Criteria { get; init; }
}
