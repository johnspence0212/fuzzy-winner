using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Models;

namespace Api.Data.Configurations;

public class WorkoutSessionConfiguration : BaseEntityConfiguration<WorkoutSession>
{
    public override void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        base.Configure(builder);

        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.WorkoutSessions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.WorkoutTemplate)
            .WithMany()
            .HasForeignKey(s => s.WorkoutTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("WorkoutSessions");
    }
}
