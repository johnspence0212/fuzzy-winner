<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { runRequest } from '@/api/base/client'
import { planApi } from '@/api/planApi'
import { scheduleApi } from '@/api/scheduleApi'
import { workoutTemplateApi } from '@/api/workoutTemplateApi'
import type { Plan, ScheduleResponse, WorkoutTemplate } from '@/api/types/schema'
import type {
  ExerciseItemRequest,
  SchedulePutRequest,
  ScheduleSlotRequest,
} from '@/api/types/workoutRequests'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const route = useRoute()
const router = useRouter()

const planId = computed(() => Number(route.params.planId))

const plan = ref<Plan | null>(null)
const templates = ref<WorkoutTemplate[]>([])
const schedule = ref<ScheduleResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const saveMessage = ref<string | null>(null)

const editPlanName = ref('')
const editPlanNotes = ref('')

const newTemplateName = ref('')
const newExercises = ref<ExerciseItemRequest[]>([
  { name: '', sortOrder: 0, targetSets: 3, targetReps: 5 },
])

const sessionsPerWeek = ref(3)
const repeatWeeks = ref(2)
const slotAssignments = ref<ScheduleSlotRequest[]>([])

const buildDefaultSlots = (
  templatesList: readonly WorkoutTemplate[],
  existing: ScheduleSlotRequest[],
): ScheduleSlotRequest[] => {
  const spw = sessionsPerWeek.value
  const rw = repeatWeeks.value
  const defaultId = templatesList[0]?.id ?? 0
  const next: ScheduleSlotRequest[] = []
  for (let w = 0; w < rw; w++) {
    for (let s = 0; s < spw; s++) {
      const prev = existing.find((e) => e.weekIndex === w && e.sessionIndex === s)
      next.push({
        weekIndex: w,
        sessionIndex: s,
        workoutTemplateId: prev?.workoutTemplateId && templatesList.some((t) => t.id === prev.workoutTemplateId)
          ? prev.workoutTemplateId
          : defaultId,
      })
    }
  }
  return next
}

const slotLabel = (w: number, s: number) =>
  `Week ${w + 1} — session ${s + 1}`

const loadAll = async () => {
  loading.value = true
  error.value = null
  try {
    const id = planId.value
    if (Number.isNaN(id)) {
      error.value = 'Invalid plan.'
      return
    }
    const [p, t, sch] = await Promise.all([
      runRequest(planApi.getById(id)),
      workoutTemplateApi.getByPlan(id),
      scheduleApi.get(id),
    ])
    plan.value = p
    editPlanName.value = p.name
    editPlanNotes.value = p.notes ?? ''
    templates.value = [...t]
    schedule.value = sch
    if (sch.sessionsPerWeek > 0 && sch.repeatWeeks > 0) {
      sessionsPerWeek.value = sch.sessionsPerWeek
      repeatWeeks.value = sch.repeatWeeks
      slotAssignments.value = sch.slots.map((sl) => ({
        weekIndex: sl.weekIndex,
        sessionIndex: sl.sessionIndex,
        workoutTemplateId: sl.workoutTemplateId,
      }))
    } else {
      slotAssignments.value = buildDefaultSlots(templates.value, [])
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load plan.'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void loadAll()
})

watch(planId, () => {
  void loadAll()
})

watch([sessionsPerWeek, repeatWeeks], () => {
  slotAssignments.value = buildDefaultSlots(templates.value, slotAssignments.value)
})

const editTargetId = ref(0)
const editTemplateName = ref('')
const editExercises = ref<ExerciseItemRequest[]>([])

watch(editTargetId, (id) => {
  if (!id) {
    editTemplateName.value = ''
    editExercises.value = []
    return
  }
  const t = templates.value.find((x) => x.id === id)
  if (!t) return
  editTemplateName.value = t.name
  editExercises.value = [...t.exercises]
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((ex) => ({
      name: ex.name,
      sortOrder: ex.sortOrder,
      targetSets: ex.targetSets,
      targetReps: ex.targetReps,
      targetWeightKg: ex.targetWeightKg ?? undefined,
    }))
})

const addEditExerciseRow = () => {
  const nextOrder = editExercises.value.length
  editExercises.value.push({
    name: '',
    sortOrder: nextOrder,
    targetSets: 3,
    targetReps: 5,
  })
}

const removeEditExerciseRow = (index: number) => {
  editExercises.value = editExercises.value.filter((_, i) => i !== index)
}

const saveEditTemplate = async () => {
  if (!editTargetId.value) {
    error.value = 'Choose a workout to edit.'
    return
  }
  const name = editTemplateName.value.trim()
  if (!name) {
    error.value = 'Workout name is required.'
    return
  }
  const exercises = editExercises.value
    .map((e, i) => ({
      ...e,
      name: e.name.trim(),
      sortOrder: i,
      targetWeightKg:
        e.targetWeightKg === undefined || Number.isNaN(Number(e.targetWeightKg))
          ? undefined
          : Number(e.targetWeightKg),
    }))
    .filter((e) => e.name.length > 0)

  if (!exercises.length) {
    error.value = 'Keep at least one exercise with a name.'
    return
  }

  error.value = null
  saveMessage.value = null
  try {
    await workoutTemplateApi.updateWithExercises(editTargetId.value, {
      name,
      sortOrder: templates.value.find((t) => t.id === editTargetId.value)?.sortOrder ?? null,
      exercises,
    })
    await loadAll()
    saveMessage.value = 'Workout updated.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not update workout.'
  }
}

const savePlanMeta = async () => {
  if (!plan.value) return
  saveMessage.value = null
  try {
    await runRequest(
      planApi.update(plan.value.id, {
        ...plan.value,
        name: editPlanName.value.trim(),
        notes: editPlanNotes.value.trim() ? editPlanNotes.value.trim() : null,
      }),
    )
    await loadAll()
    saveMessage.value = 'Plan saved.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not save plan.'
  }
}

const addExerciseRow = () => {
  const nextOrder = newExercises.value.length
  newExercises.value.push({
    name: '',
    sortOrder: nextOrder,
    targetSets: 3,
    targetReps: 5,
  })
}

const removeExerciseRow = (index: number) => {
  newExercises.value = newExercises.value.filter((_, i) => i !== index)
}

const createTemplate = async () => {
  const name = newTemplateName.value.trim()
  if (!name) {
    error.value = 'Workout name is required.'
    return
  }
  const exercises = newExercises.value
    .map((e, i) => ({
      ...e,
      name: e.name.trim(),
      sortOrder: i,
      targetWeightKg:
        e.targetWeightKg === undefined || Number.isNaN(Number(e.targetWeightKg))
          ? undefined
          : Number(e.targetWeightKg),
    }))
    .filter((e) => e.name.length > 0)

  if (!exercises.length) {
    error.value = 'Add at least one exercise with a name.'
    return
  }

  error.value = null
  saveMessage.value = null
  try {
    await workoutTemplateApi.createWithExercises({
      planId: planId.value,
      name,
      sortOrder: templates.value.length,
      exercises,
    })
    newTemplateName.value = ''
    newExercises.value = [{ name: '', sortOrder: 0, targetSets: 3, targetReps: 5 }]
    await loadAll()
    saveMessage.value = 'Workout added.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not create workout.'
  }
}

const saveSchedule = async () => {
  const ids = new Set(templates.value.map((t) => t.id))
  const invalid = slotAssignments.value.some(
    (s) => !s.workoutTemplateId || !ids.has(s.workoutTemplateId),
  )
  if (invalid || !templates.value.length) {
    error.value = 'Add workouts first, then assign each slot to a workout.'
    return
  }

  const body: SchedulePutRequest = {
    sessionsPerWeek: sessionsPerWeek.value,
    repeatWeeks: repeatWeeks.value,
    slots: [...slotAssignments.value],
  }

  error.value = null
  saveMessage.value = null
  try {
    const updated = await scheduleApi.put(planId.value, body)
    schedule.value = updated
    saveMessage.value = 'Schedule saved.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not save schedule.'
  }
}

const goBack = () => {
  void router.push({ name: 'plan' })
}
</script>

<template>
  <div class="mx-auto flex w-full max-w-lg flex-col gap-6 p-4 pb-24">
    <Button class="h-10 w-fit" variant="ghost" type="button" @click="goBack">← All plans</Button>

    <p v-if="loading" class="text-muted-foreground text-sm">Loading…</p>
    <p v-else-if="error" class="text-destructive text-sm">{{ error }}</p>

    <template v-else-if="plan">
      <header class="space-y-1">
        <h1 class="text-2xl font-semibold tracking-tight">Plan setup</h1>
        <p class="text-muted-foreground text-sm">{{ plan.name }}</p>
      </header>

      <p v-if="saveMessage" class="text-sm text-emerald-600 dark:text-emerald-400">{{ saveMessage }}</p>

      <section class="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Plan details</h2>
        <div class="space-y-2">
          <label class="text-sm font-medium" for="edit-name">Name</label>
          <Input id="edit-name" v-model="editPlanName" class="h-11 w-full" type="text" />
        </div>
        <div class="space-y-2">
          <label class="text-sm font-medium" for="edit-notes">Notes</label>
          <Input id="edit-notes" v-model="editPlanNotes" class="h-11 w-full" type="text" />
        </div>
        <Button class="h-11 w-full" type="button" variant="secondary" @click="savePlanMeta">
          Save plan details
        </Button>
      </section>

      <section class="space-y-4 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Workouts in this plan</h2>
        <ul class="flex flex-col gap-3">
          <li
            v-for="t in templates"
            :key="t.id"
            class="rounded-md border bg-background/60 px-3 py-3 text-sm"
          >
            <div class="font-medium">{{ t.name }}</div>
            <ul class="text-muted-foreground mt-2 space-y-1">
              <li v-for="ex in t.exercises" :key="ex.id">
                {{ ex.name }} — {{ ex.targetSets }}×{{ ex.targetReps }}
                <span v-if="ex.targetWeightKg != null"> @ {{ ex.targetWeightKg }} kg</span>
              </li>
            </ul>
          </li>
          <li v-if="!templates.length" class="text-muted-foreground text-sm">No workouts yet.</li>
        </ul>

        <div class="space-y-3 border-t pt-4">
          <h3 class="text-sm font-medium">Add workout</h3>
          <div class="space-y-2">
            <label class="text-sm font-medium" for="tpl-name">Workout label</label>
            <Input
              id="tpl-name"
              v-model="newTemplateName"
              class="h-11 w-full"
              placeholder="e.g. A"
            />
          </div>
          <div class="space-y-3">
            <div v-for="(row, idx) in newExercises" :key="idx" class="space-y-2 rounded-md border p-3">
              <div class="flex items-center justify-between gap-2">
                <span class="text-muted-foreground text-xs font-medium uppercase">Exercise {{ idx + 1 }}</span>
                <Button
                  v-if="newExercises.length > 1"
                  size="sm"
                  type="button"
                  variant="ghost"
                  @click="removeExerciseRow(idx)"
                >
                  Remove
                </Button>
              </div>
              <Input v-model="row.name" class="h-11 w-full" placeholder="Exercise name" />
              <div class="grid grid-cols-3 gap-2">
                <div>
                  <label class="text-muted-foreground text-xs">Sets</label>
                  <Input
                    v-model.number="row.targetSets"
                    class="h-11 w-full"
                    min="1"
                    type="number"
                  />
                </div>
                <div>
                  <label class="text-muted-foreground text-xs">Reps</label>
                  <Input
                    v-model.number="row.targetReps"
                    class="h-11 w-full"
                    min="1"
                    type="number"
                  />
                </div>
                <div>
                  <label class="text-muted-foreground text-xs">Kg (opt.)</label>
                  <Input
                    v-model.number="row.targetWeightKg"
                    class="h-11 w-full"
                    step="0.5"
                    type="number"
                  />
                </div>
              </div>
            </div>
          </div>
          <Button class="h-10 w-full" type="button" variant="outline" @click="addExerciseRow">
            Add another exercise
          </Button>
          <Button class="h-11 w-full" type="button" @click="createTemplate">Save workout</Button>
        </div>
      </section>

      <section v-if="templates.length" class="space-y-4 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Edit a workout</h2>
        <div class="space-y-2">
          <label class="text-sm font-medium" for="edit-target">Workout</label>
          <select
            id="edit-target"
            v-model.number="editTargetId"
            class="border-input bg-background h-11 w-full rounded-md border px-3 text-sm"
          >
            <option :value="0">Select…</option>
            <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }}</option>
          </select>
        </div>
        <template v-if="editTargetId !== 0">
          <div class="space-y-2">
            <label class="text-sm font-medium" for="edit-tpl-name">Label</label>
            <Input id="edit-tpl-name" v-model="editTemplateName" class="h-11 w-full" />
          </div>
          <div class="space-y-3">
            <div
              v-for="(row, idx) in editExercises"
              :key="`edit-${idx}`"
              class="space-y-2 rounded-md border p-3"
            >
              <div class="flex items-center justify-between gap-2">
                <span class="text-muted-foreground text-xs font-medium uppercase">Exercise {{ idx + 1 }}</span>
                <Button
                  v-if="editExercises.length > 1"
                  size="sm"
                  type="button"
                  variant="ghost"
                  @click="removeEditExerciseRow(idx)"
                >
                  Remove
                </Button>
              </div>
              <Input v-model="row.name" class="h-11 w-full" placeholder="Exercise name" />
              <div class="grid grid-cols-3 gap-2">
                <div>
                  <label class="text-muted-foreground text-xs">Sets</label>
                  <Input v-model.number="row.targetSets" class="h-11 w-full" min="1" type="number" />
                </div>
                <div>
                  <label class="text-muted-foreground text-xs">Reps</label>
                  <Input v-model.number="row.targetReps" class="h-11 w-full" min="1" type="number" />
                </div>
                <div>
                  <label class="text-muted-foreground text-xs">Kg</label>
                  <Input v-model.number="row.targetWeightKg" class="h-11 w-full" step="0.5" type="number" />
                </div>
              </div>
            </div>
          </div>
          <Button class="h-10 w-full" type="button" variant="outline" @click="addEditExerciseRow">
            Add exercise
          </Button>
          <Button class="h-11 w-full" type="button" @click="saveEditTemplate">Save changes</Button>
        </template>
      </section>

      <section class="space-y-4 rounded-lg border bg-card p-4 shadow-sm">
        <h2 class="text-sm font-medium">Rotating schedule</h2>
        <p class="text-muted-foreground text-sm">
          Set sessions per week and how many weeks before the pattern repeats (e.g. ABA / BAB = 3 sessions × 2
          weeks).
        </p>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="text-muted-foreground text-xs">Sessions / week</label>
            <Input v-model.number="sessionsPerWeek" class="h-11 w-full" min="1" type="number" />
          </div>
          <div>
            <label class="text-muted-foreground text-xs">Repeat (weeks)</label>
            <Input v-model.number="repeatWeeks" class="h-11 w-full" min="1" type="number" />
          </div>
        </div>

        <ul class="flex flex-col gap-3">
          <li
            v-for="slot in slotAssignments"
            :key="`${slot.weekIndex}-${slot.sessionIndex}`"
            class="flex flex-col gap-2 rounded-md border bg-background/60 p-3"
          >
            <span class="text-sm font-medium">{{ slotLabel(slot.weekIndex, slot.sessionIndex) }}</span>
            <select
              v-model.number="slot.workoutTemplateId"
              class="border-input bg-background h-11 w-full rounded-md border px-3 text-sm"
            >
              <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }}</option>
            </select>
          </li>
        </ul>

        <Button class="h-11 w-full" type="button" @click="saveSchedule">Save schedule</Button>
      </section>
    </template>
  </div>
</template>
