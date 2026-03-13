using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class GradesConfiguration : IEntityTypeConfiguration<Grade>
    {
        public void Configure(EntityTypeBuilder<Grade> builder)
        {
            builder.ToTable("grades");

            builder.HasKey(x => x.id);

            builder.Property(x => x.id)
                .ValueGeneratedNever();

            builder.Property(x => x.submissionId)
                .IsRequired();

            builder.Property(x => x.score)
                .IsRequired();

            builder.Property(x => x.verdictText)
                .IsRequired();

            builder.Property(x => x.verdictedAt)
                .IsRequired();

            builder.HasOne(x => x.submission)
                .WithOne(x => x.grade)
                .HasForeignKey<Grade>(x => x.submissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}