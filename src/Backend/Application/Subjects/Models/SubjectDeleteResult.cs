namespace Application.Subjects.Models;

public sealed record SubjectDeleteResult(SubjectDeleteStatus Status)
{
    public static SubjectDeleteResult Success()
    {
        return new SubjectDeleteResult(SubjectDeleteStatus.Success);
    }

    public static SubjectDeleteResult Forbidden()
    {
        return new SubjectDeleteResult(SubjectDeleteStatus.Forbidden);
    }

    public static SubjectDeleteResult NotFound()
    {
        return new SubjectDeleteResult(SubjectDeleteStatus.NotFound);
    }
}
