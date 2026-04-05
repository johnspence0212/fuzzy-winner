import { Effect, Schema } from 'effect'

import { httpClient, runRequest } from '@/api/base/client'
import type {
  CreateWorkoutTemplateRequest,
  UpdateWorkoutTemplateRequest,
} from '@/api/types/workoutRequests'
import { WorkoutTemplateSchema, type WorkoutTemplate } from '@/api/types/schema'

const templatesArraySchema = Schema.Array(WorkoutTemplateSchema)

export const workoutTemplateApi = {
  getByPlan(planId: number): Promise<readonly WorkoutTemplate[]> {
    return runRequest(
      httpClient.get<unknown>(`/workout-template/plan/${planId}`).pipe(
        Effect.andThen(Schema.decodeUnknown(templatesArraySchema)),
      ),
    )
  },

  createWithExercises(body: CreateWorkoutTemplateRequest): Promise<WorkoutTemplate> {
    return runRequest(
      httpClient.post<unknown>('/workout-template/with-exercises', body).pipe(
        Effect.andThen(Schema.decodeUnknown(WorkoutTemplateSchema)),
      ),
    )
  },

  updateWithExercises(
    id: number,
    body: UpdateWorkoutTemplateRequest,
  ): Promise<WorkoutTemplate> {
    return runRequest(
      httpClient
        .putJson<unknown>(`/workout-template/${id}/with-exercises`, body)
        .pipe(Effect.andThen(Schema.decodeUnknown(WorkoutTemplateSchema))),
    )
  },
}
