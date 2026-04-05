namespace Application.Submissions.Models;

public enum DecisionVoteStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidOperation,
    AlreadyVoted,
    SessionClosed,
    WrongDecisionMode
}
