namespace Application.Criteria.Models;

public sealed class CriterionUpdateResult
{
    private CriterionUpdateResult(CriterionUpdateStatus status, CriterionResponse? criterion = null)
    {
        Status = status;
        Criterion = criterion;
    }

    public CriterionUpdateStatus Status { get; }
    public CriterionResponse? Criterion { get; }

    public static CriterionUpdateResult Success(CriterionResponse criterion) => new(CriterionUpdateStatus.Success, criterion);
    public static CriterionUpdateResult NotFound() => new(CriterionUpdateStatus.NotFound);
    public static CriterionUpdateResult Forbidden() => new(CriterionUpdateStatus.Forbidden);
}

public enum CriterionUpdateStatus
{
    Success,
    NotFound,
    Forbidden
}
