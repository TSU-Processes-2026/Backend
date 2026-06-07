namespace Application.Reviews.Models;

public sealed class StartReviewRequest
{
}

public sealed class SaveDraftRequest
{
    public decimal? OverallScore { get; init; }
    public string? OverallComment { get; init; }
    public List<CriterionResultDto>? CriterionResults { get; init; }
}

public sealed class SubmitReviewRequest
{
    public decimal? OverallScore { get; init; }
    public string? OverallComment { get; init; }
    public List<CriterionResultDto>? CriterionResults { get; init; }
}

public sealed class CriterionResultDto
{
    public Guid CriterionId { get; init; }
    public decimal? Value { get; init; }
    public string? Comment { get; init; }
}
