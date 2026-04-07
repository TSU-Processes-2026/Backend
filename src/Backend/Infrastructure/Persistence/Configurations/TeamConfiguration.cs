using Infrastructure.Identity;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SubjectId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CaptainUserId)
            .IsRequired(false);

        builder.Property(x => x.SelectionMethod)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(x => x.CaptainSelectedAt)
            .IsRequired(false);

        builder.HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Captain)
            .WithMany()
            .HasForeignKey(x => x.CaptainUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Team)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.CaptainVotingSessions)
            .WithOne(x => x.Team)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
