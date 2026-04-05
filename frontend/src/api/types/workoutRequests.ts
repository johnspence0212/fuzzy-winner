export interface ExerciseItemRequest {
  name: string
  sortOrder: number
  targetSets: number
  targetReps: number
  /** Omit or leave unset when not using a target weight */
  targetWeightKg?: number
}

export interface CreateWorkoutTemplateRequest {
  planId: number
  name: string
  sortOrder?: number | null
  exercises: ExerciseItemRequest[]
}

export interface UpdateWorkoutTemplateRequest {
  name: string
  sortOrder?: number | null
  exercises: ExerciseItemRequest[]
}

export interface ScheduleSlotRequest {
  weekIndex: number
  sessionIndex: number
  workoutTemplateId: number
}

export interface SchedulePutRequest {
  sessionsPerWeek: number
  repeatWeeks: number
  slots: ScheduleSlotRequest[]
}

export interface PerformedSetRequest {
  exerciseDefinitionId?: number | null
  exerciseNameSnapshot?: string | null
  setIndex: number
  reps: number
  weightKg?: number | null
}

export interface CreateWorkoutSessionRequest {
  planId: number
  workoutTemplateId: number
  performedAt?: string | null
  notes?: string | null
  sets: PerformedSetRequest[]
}
