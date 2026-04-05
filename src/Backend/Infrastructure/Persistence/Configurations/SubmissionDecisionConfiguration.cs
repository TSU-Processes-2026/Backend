using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SubmissionDecisionConfiguration : IEntityTypeConfiguration<SubmissionDecision>
{
    public void Configure(EntityTypeBuilder<SubmissionDecision> builder)
    {
        builder.ToTable("submission_decisions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.DecisionSessionId)
            .IsRequired();

        builder.Property(x => x.DecisionMakerId)
            .IsRequired();

        builder.Property(x => x.Decision)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.DecidedAt)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.HasOne(x => x.DecisionSession)
            .WithMany(x => x.Decisions)
            .HasForeignKey(x => x.DecisionSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DecisionMaker)
            .WithMany()
            .HasForeignKey(x => x.DecisionMakerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DecisionSessionId, x.DecisionMakerId })
            .IsUnique();
    }
}
