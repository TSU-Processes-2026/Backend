namespace Infrastructure.Persistence.Entities;

public sealed class AssignmentQuestionOption
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public required string Text { get; set; }
    public AssignmentQuestion Question { get; set; } = null!;
}
