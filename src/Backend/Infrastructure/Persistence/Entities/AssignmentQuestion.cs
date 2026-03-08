namespace Infrastructure.Persistence.Entities;

public sealed class AssignmentQuestion
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public required string QuestionType { get; set; }
    public required string QuestionData { get; set; }
    public Post Assignment { get; set; } = null!;
    public ICollection<AssignmentQuestionOption> Options { get; set; } = new List<AssignmentQuestionOption>();
}
