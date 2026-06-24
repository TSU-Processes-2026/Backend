using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CriterionResultConfiguration : IEntityTypeConfiguration<CriterionResult>
{
    public void Configure(EntityTypeBuilder<CriterionResult> builder)
    {
        builder.ToTable("criterion_results");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Value)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AssessmentType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.ComputedValue)
            .HasColumnType("decimal(10,2)")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Submission)
            .WithMany(x => x.CriterionResults)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Criterion)
            .WithMany()
            .HasForeignKey(x => x.CriterionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Review)
            .WithMany(x => x.CriterionResults)
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
