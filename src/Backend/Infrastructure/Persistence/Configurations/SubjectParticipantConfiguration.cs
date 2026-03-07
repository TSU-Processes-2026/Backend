using Infrastructure.Identity;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SubjectParticipantConfiguration : IEntityTypeConfiguration<SubjectParticipant>
{
    public void Configure(EntityTypeBuilder<SubjectParticipant> builder)
    {
        builder.ToTable("subject_participants");

        builder.HasKey(x => new { x.SubjectId, x.UserId });

        builder.Property(x => x.Role)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.SubjectParticipants)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
