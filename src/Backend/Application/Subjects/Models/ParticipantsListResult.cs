namespace Application.Subjects.Models;

public sealed record ParticipantsListResult(ParticipantMutationStatus Status, IReadOnlyList<ParticipantResponse> Participants)
{
    public static ParticipantsListResult Success(IReadOnlyList<ParticipantResponse> participants)
    {
        return new ParticipantsListResult(ParticipantMutationStatus.Success, participants);
    }

    public static ParticipantsListResult Forbidden()
    {
        return new ParticipantsListResult(ParticipantMutationStatus.Forbidden, Array.Empty<ParticipantResponse>());
    }
}
