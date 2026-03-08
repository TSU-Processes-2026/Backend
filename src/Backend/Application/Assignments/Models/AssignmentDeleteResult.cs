namespace Application.Assignments.Models;

public sealed record AssignmentDeleteResult(AssignmentDeleteStatus Status)
{
    public static AssignmentDeleteResult Success()
    {
        return new AssignmentDeleteResult(AssignmentDeleteStatus.Success);
    }

    public static AssignmentDeleteResult Forbidden()
    {
        return new AssignmentDeleteResult(AssignmentDeleteStatus.Forbidden);
    }
}
