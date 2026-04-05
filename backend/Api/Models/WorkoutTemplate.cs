namespace Api.Models;

public class WorkoutTemplate : BaseEntity
{
    public int PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? SortOrder { get; set; }

    public Plan Plan { get; set; } = null!;
    public ICollection<ExerciseDefinition> Exercises { get; set; } = new List<ExerciseDefinition>();
}
