namespace Api.Models;

public class ExerciseDefinition : BaseEntity
{
    public int WorkoutTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public decimal? TargetWeightKg { get; set; }

    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;
}
