<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import { runRequest } from '@/api/base/client'
import { planApi } from '@/api/planApi'
import { scheduleApi } from '@/api/scheduleApi'
import { workoutSessionApi } from '@/api/workoutSessionApi'
import { workoutTemplateApi } from '@/api/workoutTemplateApi'
import type { Plan, ScheduleResponse, WorkoutSessionResponse, WorkoutTemplate } from '@/api/types/schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const LAST_PLAN_KEY = 'workout.lastPlanId'

interface SetInputRow {
  exerciseDefinitionId: number | null
  exerciseNameSnapshot: string
  setLabel: string
  setIndex: number
  reps: number
  weightLbs?: number
}

const plans = ref<Plan[]>([])
const selectedPlanId = ref(0)
const templates = ref<WorkoutTemplate[]>([])
const schedule = ref<ScheduleResponse | null>(null)
const recentSessions = ref<WorkoutSessionResponse[]>([])

const selectedTemplateId = ref(0)
const setRows = ref<SetInputRow[]>([])
const sessionNotes = ref('')
const showHistory = ref(false)

const loading = ref(true)
const saving = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const templatesById = computed(() => {
  const m = new Map<number, WorkoutTemplate>()
  for (const t of templates.value) m.set(t.id, t)
  return m
})

const scheduleHints = computed(() => {
  const sch = schedule.value
  if (!sch || sch.slots.length === 0) return []
  return sch.slots.map((s) => ({
    label: `Week ${s.weekIndex + 1} · session ${s.sessionIndex + 1}`,
    templateName: s.workoutTemplateName,
    templateId: s.workoutTemplateId,
  }))
})

const rebuildSets = (t: WorkoutTemplate) => {
  const rows: SetInputRow[] = []
  let setIndex = 1
  const exercises = [...t.exercises].sort((a, b) => a.sortOrder - b.sortOrder)
  for (const ex of exercises) {
    for (let s = 0; s < ex.targetSets; s++) {
      rows.push({
        exerciseDefinitionId: ex.id,
        exerciseNameSnapshot: ex.name,
        setLabel: `${ex.name} · set ${s + 1}`,
        setIndex: setIndex++,
        reps: ex.targetReps,
        ...(ex.targetWeightLbs != null ? { weightLbs: ex.targetWeightLbs } : {}),
      })
    }
  }
  setRows.value = rows
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
    schedule.value = null
    recentSessions.value = []
    selectedTemplateId.value = 0
    setRows.value = []
    return
  }
  localStorage.setItem(LAST_PLAN_KEY, String(id))
  const [t, sch, sessions] = await Promise.all([
    workoutTemplateApi.getByPlan(id),
    scheduleApi.get(id),
    workoutSessionApi.listForPlan(id, 15),
  ])
  templates.value = [...t]
  schedule.value = sch
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

watch(selectedTemplateId, (tid) => {
  if (!tid) {
    setRows.value = []
    return
  }
  const t = templatesById.value.get(tid)
  if (t) rebuildSets(t)
})

const finishWorkout = async () => {
  success.value = null
  if (!selectedPlanId.value || !selectedTemplateId.value) {
    error.value = 'Choose a plan and workout.'
    return
  }
  if (!setRows.value.length) {
    error.value = 'This workout has no sets to log. Edit it in Plan setup.'
    return
  }

  saving.value = true
  error.value = null
  try {
    await workoutSessionApi.create({
      planId: selectedPlanId.value,
      workoutTemplateId: selectedTemplateId.value,
      performedAt: new Date().toISOString(),
      notes: sessionNotes.value.trim() ? sessionNotes.value.trim() : null,
      sets: setRows.value.map((r) => ({
        exerciseDefinitionId: r.exerciseDefinitionId,
        exerciseNameSnapshot: r.exerciseNameSnapshot,
        setIndex: r.setIndex,
        reps: Number.isFinite(r.reps) ? r.reps : 0,
        weightLbs:
          r.weightLbs === undefined ||
          (typeof r.weightLbs === 'number' && Number.isNaN(r.weightLbs))
            ? null
            : r.weightLbs,
      })),
    })
    sessionNotes.value = ''
    success.value = 'Workout saved.'
    await loadPlanContext()
    const t = templatesById.value.get(selectedTemplateId.value)
    if (t) rebuildSets(t)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not save workout.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="mx-auto flex w-full max-w-lg flex-col gap-6 p-4 pb-28">
    <header class="space-y-1">
      <h1 class="text-2xl font-semibold tracking-tight">Workout</h1>
      <p class="text-muted-foreground text-sm">Log today’s session. Set up plans under Plan.</p>
    </header>

    <p v-if="loading" class="text-muted-foreground text-sm">Loading…</p>
    <template v-else>
      <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
      <p v-if="success" class="text-sm text-emerald-600 dark:text-emerald-400">{{ success }}</p>

      <section class="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Plan</h2>
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
      </section>

      <section v-if="selectedPlanId" class="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Schedule (read-only)</h2>
        <p v-if="!scheduleHints.length" class="text-muted-foreground text-sm">No schedule saved for this plan.</p>
        <ul v-else class="flex flex-col gap-2 text-sm">
          <li
            v-for="(h, i) in scheduleHints"
            :key="i"
            class="text-muted-foreground flex justify-between gap-2"
          >
            <span>{{ h.label }}</span>
            <span class="text-foreground font-medium">{{ h.templateName }}</span>
          </li>
        </ul>
      </section>

      <section v-if="selectedPlanId" class="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Today’s workout</h2>
        <select
          v-model.number="selectedTemplateId"
          class="border-input bg-background h-11 w-full rounded-md border px-3 text-sm"
        >
          <option :value="0" disabled>Select workout…</option>
          <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }}</option>
        </select>
        <p v-if="!templates.length" class="text-muted-foreground text-sm">Add workouts in Plan setup.</p>
      </section>

      <section v-if="selectedTemplateId && setRows.length" class="space-y-4 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Sets</h2>
        <div v-for="row in setRows" :key="row.setIndex" class="space-y-2 rounded-md border bg-background/60 p-3">
          <div class="text-sm font-medium">{{ row.setLabel }}</div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-muted-foreground text-xs">Reps</label>
              <Input v-model.number="row.reps" class="h-11 w-full" min="0" type="number" />
            </div>
            <div>
              <label class="text-muted-foreground text-xs">Weight (lbs)</label>
              <Input v-model.number="row.weightLbs" class="h-11 w-full" step="0.5" type="number" />
            </div>
          </div>
        </div>
        <div class="space-y-2">
          <label class="text-sm font-medium" for="session-notes">Notes (optional)</label>
          <Input id="session-notes" v-model="sessionNotes" class="h-11 w-full" placeholder="How it felt…" />
        </div>
      </section>

      <Button
        v-if="selectedTemplateId && setRows.length"
        class="h-12 w-full text-base"
        type="button"
        :disabled="saving"
        @click="finishWorkout"
      >
        {{ saving ? 'Saving…' : 'Finish workout' }}
      </Button>

      <section v-if="selectedPlanId && recentSessions.length" class="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
        <button
          class="flex w-full items-center justify-between text-left"
          type="button"
          @click="showHistory = !showHistory"
        >
          <span class="text-sm font-medium">Recent sessions</span>
          <span class="text-muted-foreground text-xs">{{ showHistory ? 'Hide' : 'Show' }}</span>
        </button>
        <ul v-if="showHistory" class="flex flex-col gap-3 text-sm">
          <li
            v-for="s in recentSessions"
            :key="s.id"
            class="text-muted-foreground border-b pb-3 last:border-0 last:pb-0"
          >
            <div class="text-foreground font-medium">
              {{ s.workoutTemplateName }} — {{ new Date(s.performedAt).toLocaleString() }}
            </div>
            <ul class="mt-1 space-y-0.5">
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
