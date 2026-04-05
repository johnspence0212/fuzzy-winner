namespace Api.Dtos;

public sealed class ExerciseItemDto
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public decimal? TargetWeightKg { get; set; }
}

public sealed class CreateWorkoutTemplateDto
{
    public int PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public List<ExerciseItemDto> Exercises { get; set; } = new();
}

public sealed class UpdateWorkoutTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
    public List<ExerciseItemDto> Exercises { get; set; } = new();
}

public sealed class ScheduleSlotDto
{
    public int WeekIndex { get; set; }
    public int SessionIndex { get; set; }
    public int WorkoutTemplateId { get; set; }
}

public sealed class SchedulePutDto
{
    public int SessionsPerWeek { get; set; }
    public int RepeatWeeks { get; set; }
    public List<ScheduleSlotDto> Slots { get; set; } = new();
}

public sealed class ScheduleResponseDto
{
    public int PlanId { get; set; }
    public int SessionsPerWeek { get; set; }
    public int RepeatWeeks { get; set; }
    public List<ScheduleSlotResponseDto> Slots { get; set; } = new();
}

public sealed class ScheduleSlotResponseDto
{
    public int WeekIndex { get; set; }
    public int SessionIndex { get; set; }
    public int WorkoutTemplateId { get; set; }
    public string? WorkoutTemplateName { get; set; }
}

public sealed class PerformedSetDto
{
    public int? ExerciseDefinitionId { get; set; }
    public string? ExerciseNameSnapshot { get; set; }
    public int SetIndex { get; set; }
    public int Reps { get; set; }
    public decimal? WeightKg { get; set; }
}

public sealed class CreateWorkoutSessionDto
{
    public int PlanId { get; set; }
    public int WorkoutTemplateId { get; set; }
    public DateTime? PerformedAt { get; set; }
    public string? Notes { get; set; }
    public List<PerformedSetDto> Sets { get; set; } = new();
}

public sealed class WorkoutSessionResponseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlanId { get; set; }
    public int WorkoutTemplateId { get; set; }
    public string? WorkoutTemplateName { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? Notes { get; set; }
    public List<PerformedSetResponseDto> Sets { get; set; } = new();
}

public sealed class PerformedSetResponseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ExerciseDefinitionId { get; set; }
    public string? ExerciseNameSnapshot { get; set; }
    public int SetIndex { get; set; }
    public int Reps { get; set; }
    public decimal? WeightKg { get; set; }
}
