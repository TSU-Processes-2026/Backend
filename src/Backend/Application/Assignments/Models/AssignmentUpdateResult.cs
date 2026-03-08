namespace Application.Assignments.Models;

public sealed record AssignmentUpdateResult(AssignmentUpdateStatus Status, AssignmentResponse? Assignment)
{
    public static AssignmentUpdateResult Success(AssignmentResponse assignment)
    {
        return new AssignmentUpdateResult(AssignmentUpdateStatus.Success, assignment);
    }

    public static AssignmentUpdateResult Forbidden()
    {
        return new AssignmentUpdateResult(AssignmentUpdateStatus.Forbidden, null);
    }
}
