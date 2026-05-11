namespace Application.Criteria.Models;

public sealed class CriterionDeleteResult
{
    private CriterionDeleteResult(CriterionDeleteStatus status)
    {
        Status = status;
    }

    public CriterionDeleteStatus Status { get; }

    public static CriterionDeleteResult Success() => new(CriterionDeleteStatus.Success);
    public static CriterionDeleteResult NotFound() => new(CriterionDeleteStatus.NotFound);
    public static CriterionDeleteResult Forbidden() => new(CriterionDeleteStatus.Forbidden);
}

public enum CriterionDeleteStatus
{
    Success,
    NotFound,
    Forbidden
}
