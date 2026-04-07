namespace Application.Submissions.Models;

public enum DecisionSessionInitiateStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidOperation,
    AlreadyActive,
    NoCaptain
}
