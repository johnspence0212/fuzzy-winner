using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Models;

namespace Api.Data.Configurations;

public class ExerciseDefinitionConfiguration : BaseEntityConfiguration<ExerciseDefinition>
{
    public override void Configure(EntityTypeBuilder<ExerciseDefinition> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TargetWeightKg).HasPrecision(10, 2);

        builder.HasOne(e => e.WorkoutTemplate)
            .WithMany(t => t.Exercises)
            .HasForeignKey(e => e.WorkoutTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("ExerciseDefinitions");
    }
}
