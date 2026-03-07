namespace Application.Subjects.Models;

public sealed record ParticipantMutationResult(ParticipantMutationStatus Status, ParticipantResponse? Participant)
{
    public static ParticipantMutationResult Success(ParticipantResponse participant)
    {
        return new ParticipantMutationResult(ParticipantMutationStatus.Success, participant);
    }

    public static ParticipantMutationResult Forbidden()
    {
        return new ParticipantMutationResult(ParticipantMutationStatus.Forbidden, null);
    }
}
