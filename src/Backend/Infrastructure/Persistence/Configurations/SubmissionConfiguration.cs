using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
    {
        public void Configure(EntityTypeBuilder<Submission> builder)
        {
            builder.ToTable("submissions");

            builder.HasKey(x => x.id);

            builder.Property(x => x.id)
                .ValueGeneratedNever();

            builder.Property(x => x.assignmentId)
                .IsRequired();

            builder.Property(x => x.authorId)
                .IsRequired();

            builder.Property(x => x.status)
                .IsRequired();

            builder.Property(x => x.submittedAt)
                .IsRequired();

            builder.HasMany(x => x.answers)
                .WithOne()
                .HasForeignKey("submissionId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.grade)
                .WithOne(x => x.submission)
                .HasForeignKey<Grade>(x => x.submissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}