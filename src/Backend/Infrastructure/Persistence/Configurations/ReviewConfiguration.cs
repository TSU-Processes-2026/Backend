using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.OverallScore)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.OverallComment)
            .HasMaxLength(2000);

        builder.Property(x => x.SubmittedAt)
            .IsRequired();

        builder.HasOne(x => x.Assignment)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
