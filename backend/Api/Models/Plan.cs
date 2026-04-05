namespace Api.Models;

public class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public ICollection<WorkoutTemplate> WorkoutTemplates { get; set; } = new List<WorkoutTemplate>();
    public ScheduleCycle? ScheduleCycle { get; set; }
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
}
