namespace Application.Teams.Models;

public sealed record StudentJoinTeamResult(StudentJoinTeamStatus Status, TeamDistributionResponse? Response, IReadOnlyList<string> Errors)
{
    public static StudentJoinTeamResult Success(TeamDistributionResponse response)
    {
        return new StudentJoinTeamResult(StudentJoinTeamStatus.Success, response, Array.Empty<string>());
    }

    public static StudentJoinTeamResult Forbidden()
    {
        return new StudentJoinTeamResult(StudentJoinTeamStatus.Forbidden, null, Array.Empty<string>());
    }

    public static StudentJoinTeamResult Invalid(IReadOnlyList<string> errors)
    {
        return new StudentJoinTeamResult(StudentJoinTeamStatus.Invalid, null, errors);
    }
}

public enum StudentJoinTeamStatus
{
    Success,
    Forbidden,
    Invalid
}
