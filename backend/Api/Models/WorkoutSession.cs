namespace Api.Models;

public class WorkoutSession : BaseEntity
{
    public int PlanId { get; set; }
    public int WorkoutTemplateId { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }

    public Plan Plan { get; set; } = null!;
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
    public ICollection<PerformedSet> PerformedSets { get; set; } = new List<PerformedSet>();
}
