namespace Application.Teams.Models;

public sealed record TeamSettingsResult(TeamSettingsStatus Status, TeamSettingsResponse? Settings, IReadOnlyList<string> Errors)
{
    public static TeamSettingsResult Success(TeamSettingsResponse settings)
    {
        return new TeamSettingsResult(TeamSettingsStatus.Success, settings, Array.Empty<string>());
    }

    public static TeamSettingsResult Forbidden()
    {
        return new TeamSettingsResult(TeamSettingsStatus.Forbidden, null, Array.Empty<string>());
    }

    public static TeamSettingsResult Invalid(IReadOnlyList<string> errors)
    {
        return new TeamSettingsResult(TeamSettingsStatus.Invalid, null, errors);
    }
}
