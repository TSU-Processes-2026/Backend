namespace Application.Teams.Models;

public sealed record CaptainInfoResponse(
    Guid TeamId,
    Guid CaptainUserId,
    CaptainSelectionMethod SelectionMethod,
    DateTimeOffset SelectedAt);
