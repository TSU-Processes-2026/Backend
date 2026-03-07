namespace Application.Subjects.Models;

public sealed record ParticipantDeleteResult(ParticipantDeleteStatus Status)
{
    public static ParticipantDeleteResult Success()
    {
        return new ParticipantDeleteResult(ParticipantDeleteStatus.Success);
    }

    public static ParticipantDeleteResult Forbidden()
    {
        return new ParticipantDeleteResult(ParticipantDeleteStatus.Forbidden);
    }
}
