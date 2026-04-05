import { Effect, Schema } from 'effect'

import { httpClient, runRequest } from '@/api/base/client'
import type { CreateWorkoutSessionRequest } from '@/api/types/workoutRequests'
import {
  WorkoutSessionResponseSchema,
  type WorkoutSessionResponse,
} from '@/api/types/schema'

const sessionsArraySchema = Schema.Array(WorkoutSessionResponseSchema)

export const workoutSessionApi = {
  create(body: CreateWorkoutSessionRequest): Promise<WorkoutSessionResponse> {
    return runRequest(
      httpClient.post<unknown>('/workout-session', body).pipe(
        Effect.andThen(Schema.decodeUnknown(WorkoutSessionResponseSchema)),
      ),
    )
  },

  listForPlan(planId: number, take = 50): Promise<readonly WorkoutSessionResponse[]> {
    return runRequest(
      httpClient.get<unknown>(`/workout-session/plan/${planId}?take=${take}`).pipe(
        Effect.andThen(Schema.decodeUnknown(sessionsArraySchema)),
      ),
    )
  },
}
