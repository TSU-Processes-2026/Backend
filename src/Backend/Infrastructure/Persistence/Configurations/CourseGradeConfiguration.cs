using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CourseGradeConfiguration : IEntityTypeConfiguration<CourseGrade>
{
    public void Configure(EntityTypeBuilder<CourseGrade> builder)
    {
        builder.ToTable("course_grades");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FinalScore)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.FinalGrade)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.CalculatedAt)
            .IsRequired();

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
