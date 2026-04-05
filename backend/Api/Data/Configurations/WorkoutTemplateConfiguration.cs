using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Models;

namespace Api.Data.Configurations;

public class WorkoutTemplateConfiguration : BaseEntityConfiguration<WorkoutTemplate>
{
    public override void Configure(EntityTypeBuilder<WorkoutTemplate> builder)
    {
        base.Configure(builder);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(t => t.Plan)
            .WithMany(p => p.WorkoutTemplates)
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.PlanId, t.Name }).IsUnique();

        builder.ToTable("WorkoutTemplates");
    }
}
