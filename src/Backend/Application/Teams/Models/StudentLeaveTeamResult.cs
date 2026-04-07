namespace Application.Teams.Models;

public sealed record StudentLeaveTeamResult(StudentLeaveTeamStatus Status, TeamDistributionResponse? Response, IReadOnlyList<string> Errors)
{
    public static StudentLeaveTeamResult Success(TeamDistributionResponse response)
    {
        return new StudentLeaveTeamResult(StudentLeaveTeamStatus.Success, response, Array.Empty<string>());
    }

    public static StudentLeaveTeamResult Forbidden()
    {
        return new StudentLeaveTeamResult(StudentLeaveTeamStatus.Forbidden, null, Array.Empty<string>());
    }

    public static StudentLeaveTeamResult Invalid(IReadOnlyList<string> errors)
    {
        return new StudentLeaveTeamResult(StudentLeaveTeamStatus.Invalid, null, errors);
    }
}

public enum StudentLeaveTeamStatus
{
    Success,
    Forbidden,
    Invalid
}
