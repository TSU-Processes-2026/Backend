namespace Application.Teams.Models;

/// <summary>
/// Request for a captain to pick a student during a draft.
/// The current captain (whose turn it is) picks a student to add to their own team.
/// No teamId is needed because the student is always added to the captain's team.
/// </summary>
public sealed class DraftPickRequest
{
    /// <summary>
    /// The ID of the student to pick. Must be an unassigned student of the subject.
    /// </summary>
    public Guid StudentId { get; init; }
}
