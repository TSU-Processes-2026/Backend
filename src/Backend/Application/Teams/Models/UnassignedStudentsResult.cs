namespace Application.Teams.Models;

public sealed record UnassignedStudentsResult(UnassignedStudentsStatus Status, UnassignedStudentsResponse? Response)
{
    public static UnassignedStudentsResult Success(UnassignedStudentsResponse response)
    {
        return new UnassignedStudentsResult(UnassignedStudentsStatus.Success, response);
    }

    public static UnassignedStudentsResult Forbidden()
    {
        return new UnassignedStudentsResult(UnassignedStudentsStatus.Forbidden, null);
    }
}
