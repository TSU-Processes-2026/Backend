namespace Application.Submissions.Models;

public enum CaptainDecisionStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidOperation,
    SessionClosed,
    WrongDecisionMode,
    NotCaptain
}
