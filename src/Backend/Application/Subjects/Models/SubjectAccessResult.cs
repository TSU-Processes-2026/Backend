namespace Application.Subjects.Models;

public sealed record SubjectAccessResult(SubjectAccessStatus Status, SubjectResponse? Subject)
{
    public static SubjectAccessResult Success(SubjectResponse subject)
    {
        return new SubjectAccessResult(SubjectAccessStatus.Success, subject);
    }

    public static SubjectAccessResult Forbidden()
    {
        return new SubjectAccessResult(SubjectAccessStatus.Forbidden, null);
    }

    public static SubjectAccessResult NotFound()
    {
        return new SubjectAccessResult(SubjectAccessStatus.NotFound, null);
    }
}
