using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class AssignmentQuestionOptionConfiguration : IEntityTypeConfiguration<AssignmentQuestionOption>
{
    public void Configure(EntityTypeBuilder<AssignmentQuestionOption> builder)
    {
        builder.ToTable("assignment_question_options");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Text)
            .IsRequired();
    }
}
