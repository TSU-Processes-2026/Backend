namespace Application.Subjects.Models;

public sealed record JoinSubjectResult(JoinSubjectStatus Status, ParticipantResponse? Participant)
{
    public static JoinSubjectResult Success(ParticipantResponse participant)
    {
        return new JoinSubjectResult(JoinSubjectStatus.Success, participant);
    }

    public static JoinSubjectResult NotFound()
    {
        return new JoinSubjectResult(JoinSubjectStatus.NotFound, null);
    }
}
