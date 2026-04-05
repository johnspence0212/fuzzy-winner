using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Models;

namespace Api.Data.Configurations;

public class ScheduledSessionConfiguration : BaseEntityConfiguration<ScheduledSession>
{
    public override void Configure(EntityTypeBuilder<ScheduledSession> builder)
    {
        base.Configure(builder);

        builder.HasOne(s => s.ScheduleCycle)
            .WithMany(c => c.ScheduledSessions)
            .HasForeignKey(s => s.ScheduleCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.WorkoutTemplate)
            .WithMany()
            .HasForeignKey(s => s.WorkoutTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ScheduleCycleId, s.WeekIndex, s.SessionIndex }).IsUnique();

        builder.ToTable("ScheduledSessions");
    }
}
