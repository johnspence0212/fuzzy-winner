using Microsoft.EntityFrameworkCore;
using Api.Models;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<ExerciseDefinition> ExerciseDefinitions => Set<ExerciseDefinition>();
    public DbSet<ScheduleCycle> ScheduleCycles => Set<ScheduleCycle>();
    public DbSet<ScheduledSession> ScheduledSessions => Set<ScheduledSession>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<PerformedSet> PerformedSets => Set<PerformedSet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations in the Configurations namespace
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}