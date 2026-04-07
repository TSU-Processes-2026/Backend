namespace Application.Teams.Models;

public sealed record CaptainVoteResponse(
    bool SessionCompleted,
    Guid? SelectedCaptainId);
