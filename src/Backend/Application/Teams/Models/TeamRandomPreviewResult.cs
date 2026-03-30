namespace Application.Teams.Models;

public sealed record TeamRandomPreviewResult(TeamRandomPreviewStatus Status, TeamRandomPreviewResponse? Response)
{
    public static TeamRandomPreviewResult Success(TeamRandomPreviewResponse response)
    {
        return new TeamRandomPreviewResult(TeamRandomPreviewStatus.Success, response);
    }

    public static TeamRandomPreviewResult Forbidden()
    {
        return new TeamRandomPreviewResult(TeamRandomPreviewStatus.Forbidden, null);
    }

    public static TeamRandomPreviewResult Invalid(TeamRandomPreviewResponse response)
    {
        return new TeamRandomPreviewResult(TeamRandomPreviewStatus.Invalid, response);
    }
}