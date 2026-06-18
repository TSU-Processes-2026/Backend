namespace Application.Reviews.Models;

public sealed class TeacherReviewRequest
{
    public decimal? OverallScore { get; init; }
    public string? OverallComment { get; init; }
    public List<CriterionResultDto>? CriterionResults { get; init; }
}

public sealed class SubmissionReviewsDto
{
    public Guid SubmissionId { get; set; }
    public List<ReviewDto> PeerReviews { get; set; } = new();
    public ReviewDto? TeacherReview { get; set; }
}

public sealed class FinalGradeDto
{
    public Guid SubmissionId { get; set; }
    public decimal? FinalScore { get; set; }
    public string? FinalSource { get; set; } = "peer";
    public DateTimeOffset? CalculatedAt { get; set; }
}
