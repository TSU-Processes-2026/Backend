namespace Application.Criteria.Models;

public sealed class CriterionListResult
{
    private CriterionListResult(CriterionListStatus status, IReadOnlyList<CriterionResponse> criteria, bool hidden)
    {
        Status = status;
        Criteria = criteria;
        Hidden = hidden;
    }

    public CriterionListStatus Status { get; }
    public IReadOnlyList<CriterionResponse> Criteria { get; }
    public bool Hidden { get; }

    public static CriterionListResult Success(IReadOnlyList<CriterionResponse> criteria, bool hidden) => new(CriterionListStatus.Success, criteria, hidden);
    public static CriterionListResult NotFound() => new(CriterionListStatus.NotFound, Array.Empty<CriterionResponse>(), false);
    public static CriterionListResult Forbidden() => new(CriterionListStatus.Forbidden, Array.Empty<CriterionResponse>(), false);
}
