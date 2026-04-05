import { Effect, Schema } from 'effect'

import { httpClient, runRequest } from '@/api/base/client'
import type { SchedulePutRequest } from '@/api/types/workoutRequests'
import { ScheduleResponseSchema, type ScheduleResponse } from '@/api/types/schema'

export const scheduleApi = {
  get(planId: number): Promise<ScheduleResponse> {
    return runRequest(
      httpClient.get<unknown>(`/plan/${planId}/schedule`).pipe(
        Effect.andThen(Schema.decodeUnknown(ScheduleResponseSchema)),
      ),
    )
  },

  put(planId: number, body: SchedulePutRequest): Promise<ScheduleResponse> {
    return runRequest(
      httpClient
        .putJson<unknown>(`/plan/${planId}/schedule`, body)
        .pipe(Effect.andThen(Schema.decodeUnknown(ScheduleResponseSchema))),
    )
  },
}
