namespace Application.Teams.Models;

public enum CaptainVoteStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidOperation,
    AlreadyVoted,
    SessionClosed,
    InvalidCandidate
}
