namespace Api.Models;

public class PerformedSet : BaseEntity
{
    public int WorkoutSessionId { get; set; }
    public int? ExerciseDefinitionId { get; set; }
    public string? ExerciseNameSnapshot { get; set; }
    public int SetIndex { get; set; }
    public int Reps { get; set; }
    public decimal? WeightLbs { get; set; }

    public WorkoutSession WorkoutSession { get; set; } = null!;
    public ExerciseDefinition? ExerciseDefinition { get; set; }
}
