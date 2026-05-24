namespace Application.Criteria.Models;

public sealed class CriterionResultsUpdateResult
{
    private CriterionResultsUpdateResult(CriterionResultsUpdateStatus status, IReadOnlyList<CriterionResultResponse> results)
    {
        Status = status;
        Results = results;
    }

    public CriterionResultsUpdateStatus Status { get; }
    public IReadOnlyList<CriterionResultResponse> Results { get; }

    public static CriterionResultsUpdateResult Success(IReadOnlyList<CriterionResultResponse> results) => new(CriterionResultsUpdateStatus.Success, results);
    public static CriterionResultsUpdateResult NotFound() => new(CriterionResultsUpdateStatus.NotFound, Array.Empty<CriterionResultResponse>());
    public static CriterionResultsUpdateResult Forbidden() => new(CriterionResultsUpdateStatus.Forbidden, Array.Empty<CriterionResultResponse>());
}
