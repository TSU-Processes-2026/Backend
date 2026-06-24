using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Title)
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.GradingMode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.SelfAssessmentEnabled)
            .IsRequired();

        builder.Property(x => x.FinalGradeScaleId)
            .IsRequired(false);

        builder.Property(x => x.PeerReviewEnabled)
            .IsRequired();

        builder.Property(x => x.PeerReviewScope)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.PeerReviewMode)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.PeerReviewDeadlinePolicy)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.TeacherFinalMode)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.PairingStrategy)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.ShowCriteriaBeforeDeadline)
            .IsRequired();

        builder.Property(x => x.LiveReviewMode)
            .IsRequired();

        builder.Property(x => x.DefaultReviewTimeLimitMinutes)
            .IsRequired(false);

        builder.HasMany(x => x.Participants)
            .WithOne(x => x.Subject)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
