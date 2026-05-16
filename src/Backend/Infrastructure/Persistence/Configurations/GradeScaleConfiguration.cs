using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
{
    public void Configure(EntityTypeBuilder<GradeScale> builder)
    {
        builder.ToTable("grade_scales");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.MinScore)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.MaxScore)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Grade)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasOne(x => x.Subject)
            .WithMany()
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
