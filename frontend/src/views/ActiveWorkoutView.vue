<script setup lang="ts">
import { X } from 'lucide-vue-next'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { workoutSessionApi } from '@/api/workoutSessionApi'
import { workoutTemplateApi } from '@/api/workoutTemplateApi'
import type { WorkoutTemplate } from '@/api/types/schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

interface SetInputRow {
  exerciseDefinitionId: number | null
  exerciseNameSnapshot: string
  setLabel: string
  setIndex: number
  reps: number
  weightLbs?: number
  baselineReps: number
  baselineWeightLbs?: number
}

interface ExerciseSetGroup {
  key: string
  name: string
  sets: SetInputRow[]
}

const route = useRoute()
const router = useRouter()

const planId = computed(() => Number(route.query.planId))
const templateId = computed(() => Number(route.query.templateId))

const template = ref<WorkoutTemplate | null>(null)
const setRows = ref<SetInputRow[]>([])
const sessionNotes = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref<string | null>(null)

const exerciseGroups = computed((): ExerciseSetGroup[] => {
  const groups: ExerciseSetGroup[] = []
  const indexByKey = new Map<string, number>()
  for (const row of setRows.value) {
    const key =
      row.exerciseDefinitionId != null
        ? `id:${row.exerciseDefinitionId}`
        : `name:${row.exerciseNameSnapshot}`
    const existing = indexByKey.get(key)
    if (existing === undefined) {
      indexByKey.set(key, groups.length)
      groups.push({ key, name: row.exerciseNameSnapshot, sets: [row] })
    } else {
      groups[existing]!.sets.push(row)
    }
  }
  return groups
})

const isDirty = computed(() => {
  if (sessionNotes.value.trim()) return true
  return setRows.value.some((row) => {
    if (row.reps !== row.baselineReps) return true
    const current =
      row.weightLbs === undefined || Number.isNaN(row.weightLbs) ? undefined : row.weightLbs
    const baseline = row.baselineWeightLbs
    return current !== baseline
  })
})

const rebuildSets = (t: WorkoutTemplate) => {
  const rows: SetInputRow[] = []
  let setIndex = 1
  const exercises = [...t.exercises].sort((a, b) => a.sortOrder - b.sortOrder)
  for (const ex of exercises) {
    for (let s = 0; s < ex.targetSets; s++) {
      const weight = ex.targetWeightLbs ?? undefined
      rows.push({
        exerciseDefinitionId: ex.id,
        exerciseNameSnapshot: ex.name,
        setLabel: `${ex.name} · set ${s + 1}`,
        setIndex: setIndex++,
        reps: ex.targetReps,
        baselineReps: ex.targetReps,
        ...(weight != null
          ? { weightLbs: weight, baselineWeightLbs: weight }
          : { baselineWeightLbs: undefined }),
      })
    }
  }
  setRows.value = rows
}

const goHome = (saved = false) => {
  void router.push({
    name: 'workout',
    ...(saved ? { query: { saved: '1' } } : {}),
  })
}

const exitSession = () => {
  if (isDirty.value && !window.confirm('Leave this workout? Unsaved changes will be lost.')) {
    return
  }
  goHome(false)
}

const finishWorkout = async () => {
  if (!planId.value || !templateId.value) {
    error.value = 'Missing workout selection.'
    return
  }
  if (!setRows.value.length) {
    error.value = 'This workout has no sets to log.'
    return
  }

  saving.value = true
  error.value = null
  try {
    await workoutSessionApi.create({
      planId: planId.value,
      workoutTemplateId: templateId.value,
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
    goHome(true)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not save workout.'
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  void (async () => {
    loading.value = true
    error.value = null
    if (!Number.isFinite(planId.value) || planId.value < 1 || !Number.isFinite(templateId.value) || templateId.value < 1) {
      goHome(false)
      return
    }
    try {
      const list = await workoutTemplateApi.getByPlan(planId.value)
      const found = list.find((t) => t.id === templateId.value) ?? null
      if (!found || !found.exercises.length) {
        goHome(false)
        return
      }
      template.value = found
      rebuildSets(found)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Could not load workout.'
    } finally {
      loading.value = false
    }
  })()
})
</script>

<template>
  <div class="bg-background flex min-h-full w-full flex-col">
    <header
      class="bg-background/95 supports-[backdrop-filter]:bg-background/80 sticky top-0 z-10 flex items-center justify-between gap-3 border-b px-4 py-3 backdrop-blur sm:px-6"
    >
      <div class="min-w-0">
        <p class="text-muted-foreground text-xs font-medium tracking-wide uppercase">Active workout</p>
        <h1 class="truncate text-xl font-semibold tracking-tight sm:text-2xl">
          {{ template?.name ?? 'Workout' }}
        </h1>
      </div>
      <Button
        class="h-10 shrink-0 gap-1.5"
        type="button"
        variant="ghost"
        :disabled="saving"
        @click="exitSession"
      >
        <X class="size-4" />
        Exit
      </Button>
    </header>

    <div class="mx-auto flex w-full max-w-3xl flex-1 flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      <p v-if="loading" class="text-muted-foreground text-sm">Loading…</p>
      <template v-else>
        <p v-if="error" class="text-destructive text-sm">{{ error }}</p>

        <section class="space-y-5">
          <div class="flex items-center justify-between gap-3">
            <h2 class="text-sm font-medium">Sets</h2>
            <p class="text-muted-foreground text-xs">
              {{ exerciseGroups.length }} exercises · {{ setRows.length }} sets
            </p>
          </div>

          <div class="flex flex-col gap-4">
            <div
              v-for="group in exerciseGroups"
              :key="group.key"
              class="space-y-3 rounded-xl border bg-card px-6 py-5 shadow-sm sm:px-8 sm:py-6"
            >
              <div class="flex items-baseline justify-between gap-3">
                <h3 class="text-base font-semibold tracking-tight">{{ group.name }}</h3>
                <span class="text-muted-foreground text-xs tabular-nums">
                  {{ group.sets.length }} {{ group.sets.length === 1 ? 'set' : 'sets' }}
                </span>
              </div>

              <div class="flex flex-col gap-2">
                <div
                  v-for="(row, setNum) in group.sets"
                  :key="row.setIndex"
                  class="grid grid-cols-[auto_1fr_1fr] items-end gap-x-2 py-1"
                >
                  <div
                    class="text-foreground flex h-11 w-6 items-center justify-center text-lg font-bold tabular-nums"
                  >
                    {{ setNum + 1 }}
                  </div>
                  <div>
                    <label class="text-muted-foreground text-xs" :for="`reps-${row.setIndex}`">
                      Reps
                    </label>
                    <Input
                      :id="`reps-${row.setIndex}`"
                      v-model.number="row.reps"
                      class="h-11 w-full"
                      min="0"
                      type="number"
                    />
                  </div>
                  <div>
                    <label class="text-muted-foreground text-xs" :for="`weight-${row.setIndex}`">
                      Weight (lbs)
                    </label>
                    <Input
                      :id="`weight-${row.setIndex}`"
                      v-model.number="row.weightLbs"
                      class="h-11 w-full"
                      step="0.5"
                      type="number"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <div class="max-w-md space-y-2">
          <label class="text-sm font-medium" for="session-notes">Notes (optional)</label>
          <Input
            id="session-notes"
            v-model="sessionNotes"
            class="h-11 w-full"
            placeholder="How it felt…"
          />
        </div>

        <div class="flex flex-col gap-3 pb-8 sm:flex-row sm:items-center">
          <Button
            class="h-12 w-full text-base sm:max-w-xs"
            type="button"
            :disabled="saving || !setRows.length"
            @click="finishWorkout"
          >
            {{ saving ? 'Saving…' : 'Finish workout' }}
          </Button>
          <Button
            class="h-12 w-full sm:w-auto"
            type="button"
            variant="outline"
            :disabled="saving"
            @click="exitSession"
          >
            Exit without saving
          </Button>
        </div>
      </template>
    </div>
  </div>
</template>
