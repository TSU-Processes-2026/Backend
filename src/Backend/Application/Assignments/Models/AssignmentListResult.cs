namespace Application.Assignments.Models;

public sealed record AssignmentListResult(AssignmentListStatus Status, IReadOnlyList<AssignmentResponse> Assignments)
{
    public static AssignmentListResult Success(IReadOnlyList<AssignmentResponse> assignments)
    {
        return new AssignmentListResult(AssignmentListStatus.Success, assignments);
    }

    public static AssignmentListResult Forbidden()
    {
        return new AssignmentListResult(AssignmentListStatus.Forbidden, Array.Empty<AssignmentResponse>());
    }
}
