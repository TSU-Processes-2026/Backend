using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class FinalGradeConfiguration : IEntityTypeConfiguration<FinalGrade>
{
    public void Configure(EntityTypeBuilder<FinalGrade> builder)
    {
        builder.ToTable("final_grades");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SubmissionId)
            .IsRequired();

        builder.Property(x => x.StudentId)
            .IsRequired(false);

        builder.Property(x => x.TeamId)
            .IsRequired(false);

        builder.Property(x => x.FinalScore)
            .HasColumnType("decimal(10,2)")
            .IsRequired(false);

        builder.Property(x => x.FinalGradeString)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.FinalSource)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.TeacherOverrideComment)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.CalculatedAt)
            .IsRequired();

        builder.HasOne(x => x.Submission)
            .WithMany()
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
