namespace Api.Models;

public class ScheduleCycle : BaseEntity
{
    public int PlanId { get; set; }
    public int SessionsPerWeek { get; set; }
    public int RepeatWeeks { get; set; }

    public Plan Plan { get; set; } = null!;
    public ICollection<ScheduledSession> ScheduledSessions { get; set; } = new List<ScheduledSession>();
}
