using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TeamGradeConfiguration : IEntityTypeConfiguration<TeamGrade>
{
    public void Configure(EntityTypeBuilder<TeamGrade> builder)
    {
        builder.ToTable("team_grades");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.AssignmentId)
            .IsRequired();

        builder.Property(x => x.SubmissionId)
            .IsRequired();

        builder.Property(x => x.RedistributeTotalScore)
            .IsRequired();

        builder.HasIndex(x => new { x.TeamId, x.AssignmentId })
            .IsUnique();

        builder.HasIndex(x => x.SubmissionId)
            .IsUnique();

        builder.HasOne(x => x.Team)
            .WithMany(x => x.TeamGrades)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Submission)
            .WithOne(x => x.TeamGrade)
            .HasForeignKey<TeamGrade>(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
