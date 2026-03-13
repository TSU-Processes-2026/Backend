using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class AnswerItemConfiguration : IEntityTypeConfiguration<AnswerItem>
    {
        public void Configure(EntityTypeBuilder<AnswerItem> builder)
        {
            builder.ToTable("answer_items");

            builder.HasKey(x => x.id);

            builder.Property(x => x.id)
                .ValueGeneratedNever();

            builder.Property(x => x.assignmentQuestionId)
                .IsRequired();

            builder.Property(x => x.answerType)
                .IsRequired();

            builder.Property(x => x.selectedOptionId);

            builder.Property(x => x.text);

            builder.Property(x => x.selectedOptionsId)
                .HasColumnType("uuid[]");

            builder.HasOne(x => x.assignmentQuestion)
                .WithMany()
                .HasForeignKey(x => x.assignmentQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}