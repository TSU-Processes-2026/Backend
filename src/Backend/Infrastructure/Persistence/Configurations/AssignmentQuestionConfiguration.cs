using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class AssignmentQuestionConfiguration : IEntityTypeConfiguration<AssignmentQuestion>
{
    public void Configure(EntityTypeBuilder<AssignmentQuestion> builder)
    {
        builder.ToTable("assignment_questions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.QuestionType)
            .IsRequired();

        builder.Property(x => x.QuestionData)
            .IsRequired();

        builder.HasMany(x => x.Options)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
