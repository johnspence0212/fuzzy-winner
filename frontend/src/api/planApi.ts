import { Schema } from 'effect'

import { BaseApiService } from '@/api/base/base'
import { PlanSchema, type Plan } from '@/api/types/schema'

export const planApi = new BaseApiService(
  'plan',
  PlanSchema as unknown as Schema.Schema<Plan>,
)
