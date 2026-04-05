using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CaptainVoteConfiguration : IEntityTypeConfiguration<CaptainVote>
{
    public void Configure(EntityTypeBuilder<CaptainVote> builder)
    {
        builder.ToTable("captain_votes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.VotingSessionId)
            .IsRequired();

        builder.Property(x => x.VoterId)
            .IsRequired();

        builder.Property(x => x.VotedForUserId)
            .IsRequired();

        builder.Property(x => x.VotedAt)
            .IsRequired();

        builder.HasOne(x => x.VotingSession)
            .WithMany(x => x.Votes)
            .HasForeignKey(x => x.VotingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Voter)
            .WithMany()
            .HasForeignKey(x => x.VoterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VotedFor)
            .WithMany()
            .HasForeignKey(x => x.VotedForUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.VotingSessionId, x.VoterId })
            .IsUnique();
    }
}
