using Infrastructure.Identity;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class TeamMemberGradeAdjustmentConfiguration : IEntityTypeConfiguration<TeamMemberGradeAdjustment>
{
    public void Configure(EntityTypeBuilder<TeamMemberGradeAdjustment> builder)
    {
        builder.ToTable("team_member_grade_adjustments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TeamGradeId)
            .IsRequired();

        builder.Property(x => x.StudentId)
            .IsRequired();

        builder.Property(x => x.Score)
            .IsRequired();

        builder.Property(x => x.AdjustedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.TeamGradeId, x.StudentId })
            .IsUnique();

        builder.HasOne(x => x.TeamGrade)
            .WithMany(x => x.MemberGradeAdjustments)
            .HasForeignKey(x => x.TeamGradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}