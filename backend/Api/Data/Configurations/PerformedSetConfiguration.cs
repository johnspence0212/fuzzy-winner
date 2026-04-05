using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Models;

namespace Api.Data.Configurations;

public class PerformedSetConfiguration : BaseEntityConfiguration<PerformedSet>
{
    public override void Configure(EntityTypeBuilder<PerformedSet> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.ExerciseNameSnapshot).HasMaxLength(200);
        builder.Property(p => p.WeightKg).HasPrecision(10, 2);

        builder.HasOne(p => p.WorkoutSession)
            .WithMany(s => s.PerformedSets)
            .HasForeignKey(p => p.WorkoutSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ExerciseDefinition)
            .WithMany()
            .HasForeignKey(p => p.ExerciseDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("PerformedSets");
    }
}
