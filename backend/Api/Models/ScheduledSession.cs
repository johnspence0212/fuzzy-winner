namespace Api.Models;

public class ScheduledSession : BaseEntity
{
    public int ScheduleCycleId { get; set; }
    public int WeekIndex { get; set; }
    public int SessionIndex { get; set; }
    public int WorkoutTemplateId { get; set; }

    public ScheduleCycle ScheduleCycle { get; set; } = null!;
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
}
