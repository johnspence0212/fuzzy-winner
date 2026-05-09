import { Schema } from 'effect'

/** .NET often omits null properties when serializing; allow missing keys. */
const optionalNullOr = <A extends Schema.Schema.Any>(s: A) => Schema.optional(Schema.NullOr(s))

export const BaseEntitySchema = Schema.Struct({
  id: Schema.Number,
  createdAt: Schema.DateFromString,
})

export const PlanSchema = Schema.Struct({
  id: Schema.Number,
  createdAt: Schema.DateFromString,
  name: Schema.String,
  notes: optionalNullOr(Schema.String),
})

export const ExerciseDefinitionSchema = Schema.Struct({
  id: Schema.Number,
  createdAt: Schema.DateFromString,
  workoutTemplateId: Schema.Number,
  name: Schema.String,
  sortOrder: Schema.Number,
  targetSets: Schema.Number,
  targetReps: Schema.Number,
  targetWeightLbs: optionalNullOr(Schema.Number),
})

export const WorkoutTemplateSchema = Schema.Struct({
  id: Schema.Number,
  createdAt: Schema.DateFromString,
  planId: Schema.Number,
  name: Schema.String,
  sortOrder: optionalNullOr(Schema.Number),
  exercises: Schema.Array(ExerciseDefinitionSchema),
})

export const ScheduleSlotResponseSchema = Schema.Struct({
  weekIndex: Schema.Number,
  sessionIndex: Schema.Number,
  workoutTemplateId: Schema.Number,
  workoutTemplateName: optionalNullOr(Schema.String),
})

export const ScheduleResponseSchema = Schema.Struct({
  planId: Schema.Number,
  sessionsPerWeek: Schema.Number,
  repeatWeeks: Schema.Number,
  slots: Schema.Array(ScheduleSlotResponseSchema),
})

export const PerformedSetResponseSchema = Schema.Struct({
  id: Schema.Number,
  createdAt: Schema.DateFromString,
  exerciseDefinitionId: optionalNullOr(Schema.Number),
  exerciseNameSnapshot: optionalNullOr(Schema.String),
  setIndex: Schema.Number,
  reps: Schema.Number,
  weightLbs: optionalNullOr(Schema.Number),
})

export const WorkoutSessionResponseSchema = Schema.Struct({
  id: Schema.Number,
  createdAt: Schema.DateFromString,
  planId: Schema.Number,
  workoutTemplateId: Schema.Number,
  workoutTemplateName: optionalNullOr(Schema.String),
  performedAt: Schema.DateFromString,
  notes: optionalNullOr(Schema.String),
  sets: Schema.Array(PerformedSetResponseSchema),
})

export type Plan = Schema.Schema.Type<typeof PlanSchema>
export type ExerciseDefinition = Schema.Schema.Type<typeof ExerciseDefinitionSchema>
export type WorkoutTemplate = Schema.Schema.Type<typeof WorkoutTemplateSchema>
export type ScheduleResponse = Schema.Schema.Type<typeof ScheduleResponseSchema>
export type WorkoutSessionResponse = Schema.Schema.Type<typeof WorkoutSessionResponseSchema>
