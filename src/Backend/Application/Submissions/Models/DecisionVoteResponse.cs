namespace Application.Submissions.Models;

public sealed record DecisionVoteResponse(bool SessionCompleted, DecisionResult? FinalResult);
