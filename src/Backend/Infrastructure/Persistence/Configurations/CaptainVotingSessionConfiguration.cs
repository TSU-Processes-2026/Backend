using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CaptainVotingSessionConfiguration : IEntityTypeConfiguration<CaptainVotingSession>
{
    public void Configure(EntityTypeBuilder<CaptainVotingSession> builder)
    {
        builder.ToTable("captain_voting_sessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.DeadlineAt)
            .IsRequired();

        builder.Property(x => x.ClosedAt)
            .IsRequired(false);

        builder.Property(x => x.IsClosed)
            .IsRequired();

        builder.Property(x => x.WinnerId)
            .IsRequired(false);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.CaptainVotingSessions)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Winner)
            .WithMany()
            .HasForeignKey(x => x.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TeamId, x.IsClosed });
    }
}
