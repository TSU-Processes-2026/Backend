using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SubjectTeamSettingsConfiguration : IEntityTypeConfiguration<SubjectTeamSettings>
{
    public void Configure(EntityTypeBuilder<SubjectTeamSettings> builder)
    {
        builder.ToTable("subject_team_settings");

        builder.HasKey(x => x.SubjectId);

        builder.Property(x => x.SubjectId)
            .ValueGeneratedNever();

        builder.Property(x => x.DistributionMode)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.FixedTeamsCount)
            .IsRequired(false);

        builder.Property(x => x.FixedTeamSize)
            .IsRequired(false);

        builder.Property(x => x.MinTeamSize)
            .IsRequired(false);

        builder.Property(x => x.MaxTeamSize)
            .IsRequired(false);

        builder.Property(x => x.IsFinalized)
            .IsRequired();

        builder.Property(x => x.FinalizedAt)
            .IsRequired(false);

        builder.Property(x => x.CaptainSelectionMode)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(x => x.CaptainVotingDeadlineDays)
            .IsRequired(false);

        builder.Property(x => x.RequiresCaptain)
            .IsRequired();

        builder.Property(x => x.DecisionMode)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(x => x.DecisionDeadlineDays)
            .IsRequired(false);

        builder.Property(x => x.RequiredDecisionVotes)
            .IsRequired(false);

        builder.Property(x => x.RequiresDecision)
            .IsRequired();

        builder.HasOne(x => x.Subject)
            .WithOne()
            .HasForeignKey<SubjectTeamSettings>(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
