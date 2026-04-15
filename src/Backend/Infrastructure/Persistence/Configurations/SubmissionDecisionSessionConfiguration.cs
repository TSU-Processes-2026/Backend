using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SubmissionDecisionSessionConfiguration : IEntityTypeConfiguration<SubmissionDecisionSession>
{
    public void Configure(EntityTypeBuilder<SubmissionDecisionSession> builder)
    {
        builder.ToTable("submission_decision_sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SubmissionId)
            .IsRequired();

        builder.Property(x => x.Mode)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.RequiredDecisionsCount)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.DeadlineAt)
            .IsRequired();

        builder.Property(x => x.ClosedAt)
            .IsRequired(false);

        builder.Property(x => x.IsClosed)
            .IsRequired();

        builder.Property(x => x.Result)
            .HasConversion<string>()
            .IsRequired(false);

        builder.HasOne(x => x.Submission)
            .WithOne(x => x.DecisionSession)
            .HasForeignKey<SubmissionDecisionSession>(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.IsClosed, x.DeadlineAt });
    }
}
