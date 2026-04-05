using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DraftStateConfiguration : IEntityTypeConfiguration<DraftState>
{
    public void Configure(EntityTypeBuilder<DraftState> builder)
    {
        builder.ToTable("draft_states");

        builder.HasKey(x => x.SubjectId);

        builder.Property(x => x.SubjectId)
            .ValueGeneratedNever();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.IsCompleted)
            .IsRequired();

        builder.Property(x => x.CurrentCaptainIndex)
            .IsRequired();

        builder.Property(x => x.CurrentRound)
            .IsRequired();

        builder.Property(x => x.CaptainOrder)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .IsRequired(false);

        builder.HasOne(x => x.Subject)
            .WithOne()
            .HasForeignKey<DraftState>(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
