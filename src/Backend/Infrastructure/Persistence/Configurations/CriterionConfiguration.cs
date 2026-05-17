using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CriterionConfiguration : IEntityTypeConfiguration<Criterion>
{
    public void Configure(EntityTypeBuilder<Criterion> builder)
    {
        builder.ToTable("criteria");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.Format)
            .IsRequired();

        builder.Property(x => x.Weight)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.MaxPoints)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Points)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Order)
            .IsRequired();

        builder.HasOne(x => x.Task)
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
