using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Models;

namespace Api.Data.Configurations;

public class ScheduleCycleConfiguration : BaseEntityConfiguration<ScheduleCycle>
{
    public override void Configure(EntityTypeBuilder<ScheduleCycle> builder)
    {
        base.Configure(builder);

        builder.HasOne(c => c.Plan)
            .WithOne(p => p.ScheduleCycle)
            .HasForeignKey<ScheduleCycle>(c => c.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.PlanId).IsUnique();

        builder.ToTable("ScheduleCycles");
    }
}
