namespace Application.Teams.Models;

public sealed record TeamValidationResult(TeamValidationStatus Status, TeamValidationResponse? Validation)
{
    public static TeamValidationResult Success(TeamValidationResponse validation)
    {
        return new TeamValidationResult(TeamValidationStatus.Success, validation);
    }

    public static TeamValidationResult Forbidden()
    {
        return new TeamValidationResult(TeamValidationStatus.Forbidden, null);
    }
}
