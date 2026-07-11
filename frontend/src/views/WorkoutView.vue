<script setup lang="ts">
import { Activity, Dumbbell, Flame, Weight } from 'lucide-vue-next'
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { runRequest } from '@/api/base/client'
import { planApi } from '@/api/planApi'
import { workoutSessionApi } from '@/api/workoutSessionApi'
import { workoutTemplateApi } from '@/api/workoutTemplateApi'
import type { Plan, WorkoutSessionResponse, WorkoutTemplate } from '@/api/types/schema'
import { Button } from '@/components/ui/button'

const LAST_PLAN_KEY = 'workout.lastPlanId'

const router = useRouter()
const route = useRoute()

const plans = ref<Plan[]>([])
const selectedPlanId = ref(0)
const templates = ref<WorkoutTemplate[]>([])
const recentSessions = ref<WorkoutSessionResponse[]>([])
const selectedTemplateId = ref(0)
const showHistory = ref(false)

const loading = ref(true)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const selectedTemplate = computed(() =>
  templates.value.find((t) => t.id === selectedTemplateId.value) ?? null,
)

const canStart = computed(() => {
  const t = selectedTemplate.value
  return Boolean(selectedPlanId.value && t && t.exercises.length > 0)
})

const totalWorkouts = computed(() => recentSessions.value.length)

const totalWeightLifted = computed(() => {
  let total = 0
  for (const session of recentSessions.value) {
    for (const set of session.sets) {
      if (set.weightLbs != null && set.reps > 0) {
        total += set.weightLbs * set.reps
      }
    }
  }
  return total
})

/** Consecutive calendar days with at least one session, counting back from today or the most recent session day. */
const currentStreak = computed(() => {
  const days = new Set(
    recentSessions.value.map((s) => {
      const d = new Date(s.performedAt)
      return `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`
    }),
  )
  if (days.size === 0) return 0

  const cursor = new Date()
  cursor.setHours(0, 0, 0, 0)
  const key = (d: Date) => `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`

  if (!days.has(key(cursor))) {
    const latest = recentSessions.value
      .map((s) => new Date(s.performedAt).getTime())
      .reduce((a, b) => Math.max(a, b), 0)
    cursor.setTime(latest)
    cursor.setHours(0, 0, 0, 0)
  }

  let streak = 0
  while (days.has(key(cursor))) {
    streak++
    cursor.setDate(cursor.getDate() - 1)
  }
  return streak
})

const formatWeight = (lbs: number) => {
  if (lbs >= 1000) {
    return `${(lbs / 1000).toFixed(lbs >= 10000 ? 0 : 1)}k`
  }
  return lbs.toLocaleString(undefined, { maximumFractionDigits: 0 })
}

const loadPlans = async () => {
  const list = await runRequest(planApi.getAll())
  plans.value = [...list]
  if (!selectedPlanId.value && plans.value.length) {
    const stored = Number(localStorage.getItem(LAST_PLAN_KEY) ?? '0')
    const exists = plans.value.some((p) => p.id === stored)
    const first = plans.value[0]
    selectedPlanId.value = exists ? stored : first?.id ?? 0
  }
}

const loadPlanContext = async () => {
  const id = selectedPlanId.value
  if (!id) {
    templates.value = []
    recentSessions.value = []
    selectedTemplateId.value = 0
    return
  }
  localStorage.setItem(LAST_PLAN_KEY, String(id))
  const [t, sessions] = await Promise.all([
    workoutTemplateApi.getByPlan(id),
    workoutSessionApi.listForPlan(id, 100),
  ])
  templates.value = [...t]
  recentSessions.value = [...sessions]
  if (!t.some((x) => x.id === selectedTemplateId.value)) {
    selectedTemplateId.value = t[0]?.id ?? 0
  }
}

const init = async () => {
  loading.value = true
  error.value = null
  try {
    await loadPlans()
    await loadPlanContext()
    if (route.query.saved === '1') {
      success.value = 'Workout saved.'
      const { saved: _saved, ...rest } = route.query
      void router.replace({ query: rest })
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not load data.'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void init()
})

watch(selectedPlanId, () => {
  void (async () => {
    if (!loading.value) {
      try {
        await loadPlanContext()
      } catch (e) {
        error.value = e instanceof Error ? e.message : 'Could not load plan.'
      }
    }
  })()
})

const startWorkout = () => {
  if (!canStart.value) return
  void router.push({
    name: 'workout-active',
    query: {
      planId: String(selectedPlanId.value),
      templateId: String(selectedTemplateId.value),
    },
  })
}
</script>

<template>
  <div class="flex w-full flex-col gap-6 p-4 pb-28 sm:p-6 lg:px-8 lg:py-6">
    <header class="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
      <div class="space-y-1">
        <h1 class="text-2xl font-semibold tracking-tight sm:text-3xl">Workout</h1>
        <p class="text-muted-foreground text-sm">Pick today’s session, then start logging.</p>
      </div>
    </header>

    <p v-if="loading" class="text-muted-foreground text-sm">Loading…</p>
    <template v-else>
      <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
      <p v-if="success" class="text-sm text-emerald-600 dark:text-emerald-400">{{ success }}</p>

      <!-- Stats -->
      <section class="grid grid-cols-1 gap-3 sm:grid-cols-3 sm:gap-4">
        <div class="rounded-xl border bg-card p-4 shadow-sm sm:p-5">
          <div class="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
            <Dumbbell class="h-3.5 w-3.5" />
            Total workouts
          </div>
          <p class="mt-2 text-3xl font-semibold tracking-tight tabular-nums">
            {{ totalWorkouts }}
          </p>
          <p class="text-muted-foreground mt-1 text-xs">Completed sessions</p>
        </div>

        <div class="rounded-xl border bg-card p-4 shadow-sm sm:p-5">
          <div class="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
            <Weight class="h-3.5 w-3.5" />
            Total weight lifted
          </div>
          <p class="mt-2 text-3xl font-semibold tracking-tight tabular-nums">
            {{ formatWeight(totalWeightLifted) }}
            <span class="text-muted-foreground text-base font-medium">lb</span>
          </p>
          <p class="text-muted-foreground mt-1 text-xs">Volume · reps × weight</p>
        </div>

        <div class="rounded-xl border bg-card p-4 shadow-sm sm:p-5">
          <div class="text-muted-foreground flex items-center gap-2 text-xs font-medium tracking-wide uppercase">
            <Flame class="h-3.5 w-3.5" />
            Current streak
          </div>
          <p class="mt-2 text-3xl font-semibold tracking-tight tabular-nums">
            {{ currentStreak }}
            <span class="text-muted-foreground text-base font-medium">
              {{ currentStreak === 1 ? 'day' : 'days' }}
            </span>
          </p>
          <p class="text-muted-foreground mt-1 text-xs">
            {{ currentStreak > 0 ? 'Keep showing up' : 'Log a session to start' }}
          </p>
        </div>
      </section>

      <!-- Setup -->
      <section class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div class="space-y-3 rounded-xl border bg-card p-4 shadow-sm sm:p-5">
          <h2 class="flex items-center gap-2 text-sm font-medium">
            <Activity class="text-muted-foreground h-4 w-4" />
            Plan
          </h2>
          <select
            v-model.number="selectedPlanId"
            class="border-input bg-background h-11 w-full rounded-md border px-3 text-sm"
          >
            <option :value="0" disabled>Select…</option>
            <option v-for="p in plans" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
          <p v-if="!plans.length" class="text-muted-foreground text-sm">
            No plans yet.
            <router-link class="text-primary font-medium underline-offset-2 hover:underline" to="/plan">
              Create a plan
            </router-link>
            first.
          </p>
        </div>

        <div v-if="selectedPlanId" class="space-y-3 rounded-xl border bg-card p-4 shadow-sm sm:p-5">
          <h2 class="text-sm font-medium">Today’s workout</h2>
          <select
            v-model.number="selectedTemplateId"
            class="border-input bg-background h-11 w-full rounded-md border px-3 text-sm"
          >
            <option :value="0" disabled>Select workout…</option>
            <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select>
          <p v-if="!templates.length" class="text-muted-foreground text-sm">Add workouts in Plan setup.</p>
          <p
            v-else-if="selectedTemplate && !selectedTemplate.exercises.length"
            class="text-muted-foreground text-sm"
          >
            This workout has no exercises yet. Add some in Plan setup.
          </p>
        </div>
      </section>

      <section
        v-if="selectedPlanId && templates.length"
        class="flex w-full flex-col items-center gap-2"
      >
        <Button
          class="h-14 w-full text-base font-semibold tracking-wide uppercase"
          type="button"
          :disabled="!canStart"
          @click="startWorkout"
        >
          Start Workout
        </Button>
        <p v-if="selectedTemplate" class="text-muted-foreground text-center text-xs">
          {{ selectedTemplate.name }} · {{ selectedTemplate.exercises.length }}
          {{ selectedTemplate.exercises.length === 1 ? 'exercise' : 'exercises' }}
        </p>
      </section>

      <!-- History -->
      <section
        v-if="selectedPlanId && recentSessions.length"
        class="space-y-3 rounded-xl border bg-card p-4 shadow-sm sm:p-5"
      >
        <button
          class="flex w-full items-center justify-between text-left"
          type="button"
          @click="showHistory = !showHistory"
        >
          <span class="text-sm font-medium">Recent sessions</span>
          <span class="text-muted-foreground text-xs">{{ showHistory ? 'Hide' : 'Show' }}</span>
        </button>
        <ul v-if="showHistory" class="grid grid-cols-1 gap-3 text-sm lg:grid-cols-2">
          <li
            v-for="s in recentSessions"
            :key="s.id"
            class="bg-muted/30 rounded-lg border p-3"
          >
            <div class="text-foreground font-medium">
              {{ s.workoutTemplateName }} —
              {{ new Date(s.performedAt).toLocaleString() }}
            </div>
            <ul class="text-muted-foreground mt-1 space-y-0.5">
              <li v-for="set in s.sets" :key="set.id">
                {{ set.exerciseNameSnapshot ?? 'Set' }} · {{ set.reps }} reps
                <span v-if="set.weightLbs != null"> @ {{ set.weightLbs }} lb</span>
              </li>
            </ul>
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>
