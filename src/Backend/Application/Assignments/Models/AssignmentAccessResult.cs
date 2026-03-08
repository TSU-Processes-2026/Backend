namespace Application.Assignments.Models;

public sealed record AssignmentAccessResult(AssignmentAccessStatus Status, AssignmentResponse? Assignment)
{
    public static AssignmentAccessResult Success(AssignmentResponse assignment)
    {
        return new AssignmentAccessResult(AssignmentAccessStatus.Success, assignment);
    }

    public static AssignmentAccessResult Forbidden()
    {
        return new AssignmentAccessResult(AssignmentAccessStatus.Forbidden, null);
    }
}
