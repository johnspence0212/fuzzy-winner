<script setup lang="ts">
import { Plus } from 'lucide-vue-next'
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@/components/ui/sheet'

const route = useRoute()
const router = useRouter()

const planId = computed(() => Number(route.params.planId))

const plan = ref<Plan | null>(null)
const templates = ref<WorkoutTemplate[]>([])
const schedule = ref<ScheduleResponse | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const saveMessage = ref<string | null>(null)

const savingPlan = ref(false)
const savingEdit = ref(false)
const savingSchedule = ref(false)
const creatingWorkout = ref(false)

const editPlanName = ref('')
const editPlanNotes = ref('')

const addWorkoutOpen = ref(false)
const addWorkoutError = ref<string | null>(null)
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

watch(addWorkoutOpen, (open) => {
  if (!open) {
    addWorkoutError.value = null
    newTemplateName.value = ''
    newExercises.value = [{ name: '', sortOrder: 0, targetSets: 3, targetReps: 5 }]
  }
})

const editTargetId = ref(0)
const editTemplateName = ref('')
const editExercises = ref<ExerciseItemRequest[]>([])

const hydrateEditFromTemplate = (id: number) => {
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
      targetWeightLbs: ex.targetWeightLbs ?? undefined,
    }))
}

watch(editTargetId, (id) => {
  hydrateEditFromTemplate(id)
})

watch(templates, () => {
  const id = editTargetId.value
  if (!id) return
  if (!templates.value.some((x) => x.id === id)) {
    editTargetId.value = 0
    return
  }
  hydrateEditFromTemplate(id)
})

const editDialogOpen = computed({
  get: () => editTargetId.value !== 0,
  set: (open: boolean) => {
    if (!open) {
      editTargetId.value = 0
    }
  },
})

const openWorkoutEdit = (id: number) => {
  error.value = null
  editTargetId.value = id
}

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
      targetWeightLbs:
        e.targetWeightLbs === undefined || Number.isNaN(Number(e.targetWeightLbs))
          ? undefined
          : Number(e.targetWeightLbs),
    }))
    .filter((e) => e.name.length > 0)

  if (!exercises.length) {
    error.value = 'Keep at least one exercise with a name.'
    return
  }

  error.value = null
  saveMessage.value = null
  savingEdit.value = true
  try {
    await workoutTemplateApi.updateWithExercises(editTargetId.value, {
      name,
      sortOrder: templates.value.find((t) => t.id === editTargetId.value)?.sortOrder ?? null,
      exercises,
    })
    await loadAll()
    saveMessage.value = 'Workout updated.'
    editTargetId.value = 0
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not update workout.'
  } finally {
    savingEdit.value = false
  }
}

const cancelWorkoutEdit = () => {
  error.value = null
  editTargetId.value = 0
}

const savePlanMeta = async () => {
  if (!plan.value) return
  saveMessage.value = null
  error.value = null
  savingPlan.value = true
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
  } finally {
    savingPlan.value = false
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
    addWorkoutError.value = 'Workout name is required.'
    return
  }
  const exercises = newExercises.value
    .map((e, i) => ({
      ...e,
      name: e.name.trim(),
      sortOrder: i,
      targetWeightLbs:
        e.targetWeightLbs === undefined || Number.isNaN(Number(e.targetWeightLbs))
          ? undefined
          : Number(e.targetWeightLbs),
    }))
    .filter((e) => e.name.length > 0)

  if (!exercises.length) {
    addWorkoutError.value = 'Add at least one exercise with a name.'
    return
  }

  addWorkoutError.value = null
  saveMessage.value = null
  creatingWorkout.value = true
  try {
    await workoutTemplateApi.createWithExercises({
      planId: planId.value,
      name,
      sortOrder: templates.value.length,
      exercises,
    })
    addWorkoutOpen.value = false
    await loadAll()
    saveMessage.value = 'Workout added.'
  } catch (e) {
    addWorkoutError.value = e instanceof Error ? e.message : 'Could not create workout.'
  } finally {
    creatingWorkout.value = false
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
  savingSchedule.value = true
  try {
    const updated = await scheduleApi.put(planId.value, body)
    schedule.value = updated
    saveMessage.value = 'Schedule saved.'
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not save schedule.'
  } finally {
    savingSchedule.value = false
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
    <p v-else-if="error && !plan" class="text-destructive text-sm">{{ error }}</p>

    <template v-else-if="plan">
      <!-- Title row: what you’re editing + explicit save -->
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
        <div class="min-w-0 space-y-1">
          <h1 class="text-2xl font-semibold tracking-tight">Plan setup</h1>
          <p class="text-muted-foreground text-sm">
            Saves the <span class="text-foreground font-medium">plan name and notes</span> below.
          </p>
        </div>
        <Button
          class="h-11 shrink-0 sm:min-w-[7.5rem]"
          type="button"
          :disabled="savingPlan"
          @click="savePlanMeta"
        >
          {{ savingPlan ? 'Saving…' : 'Save plan' }}
        </Button>
      </div>

      <p v-if="saveMessage" class="text-sm text-emerald-600 dark:text-emerald-400">{{ saveMessage }}</p>
      <p v-if="error && plan" class="text-destructive text-sm">{{ error }}</p>

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
      </section>

      <section class="space-y-4 rounded-lg border bg-card p-4 shadow-sm">
        <div class="flex items-center justify-between gap-3">
          <h2 class="text-sm font-medium">Workouts</h2>
          <Sheet v-model:open="addWorkoutOpen">
            <SheetTrigger as-child>
              <Button
                class="h-11 w-11 shrink-0 rounded-full shadow-sm"
                size="icon"
                type="button"
                aria-label="Add workout"
              >
                <Plus class="size-5" />
              </Button>
            </SheetTrigger>
            <SheetContent class="max-h-[90vh] overflow-y-auto rounded-t-2xl px-4 pb-8" side="bottom">
              <SheetHeader class="text-left">
                <SheetTitle>Add workout</SheetTitle>
                <SheetDescription>
                  A labeled day (e.g. A or B) and the exercises in it.
                </SheetDescription>
              </SheetHeader>
              <div class="flex flex-col gap-4 py-2">
                <p v-if="addWorkoutError" class="text-destructive text-sm">{{ addWorkoutError }}</p>
                <div class="space-y-2">
                  <label class="text-sm font-medium" for="sheet-tpl-name">Workout label</label>
                  <Input
                    id="sheet-tpl-name"
                    v-model="newTemplateName"
                    class="h-11 w-full"
                    placeholder="e.g. A"
                  />
                </div>
                <div class="space-y-3">
                  <div
                    v-for="(row, idx) in newExercises"
                    :key="idx"
                    class="space-y-2 rounded-md border p-3"
                  >
                    <div class="flex items-center justify-between gap-2">
                      <span class="text-muted-foreground text-xs font-medium uppercase">
                        Exercise {{ idx + 1 }}
                      </span>
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
                        <label class="text-muted-foreground text-xs">Lbs</label>
                        <Input
                          v-model.number="row.targetWeightLbs"
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
              </div>
              <SheetFooter class="mt-2 flex-col gap-2 sm:flex-col">
                <Button
                  class="h-11 w-full"
                  type="button"
                  :disabled="creatingWorkout"
                  @click="createTemplate"
                >
                  {{ creatingWorkout ? 'Saving…' : 'Save workout' }}
                </Button>
                <Button
                  class="h-11 w-full"
                  type="button"
                  variant="outline"
                  @click="addWorkoutOpen = false"
                >
                  Cancel
                </Button>
              </SheetFooter>
            </SheetContent>
          </Sheet>
        </div>

        <ul class="flex flex-col gap-3">
          <li
            v-for="t in templates"
            :key="t.id"
            class="hover:bg-muted/40 cursor-pointer rounded-md border bg-background/60 text-sm transition-colors"
            role="button"
            tabindex="0"
            :aria-label="`Edit workout ${t.name}`"
            @click="openWorkoutEdit(t.id)"
            @keydown.enter.prevent="openWorkoutEdit(t.id)"
            @keydown.space.prevent="openWorkoutEdit(t.id)"
          >
            <div class="px-3 py-3">
              <div class="flex items-start justify-between gap-2">
                <div class="font-medium">{{ t.name }}</div>
                <span class="text-muted-foreground shrink-0 text-xs">Edit</span>
              </div>
              <ul class="text-muted-foreground mt-2 space-y-1">
                <li v-for="ex in t.exercises" :key="ex.id">
                  {{ ex.name }} — {{ ex.targetSets }}×{{ ex.targetReps }}
                  <span v-if="ex.targetWeightLbs != null"> @ {{ ex.targetWeightLbs }} lb</span>
                </li>
              </ul>
            </div>
          </li>
          <li v-if="!templates.length" class="text-muted-foreground text-sm">
            No workouts yet. Tap + to add one.
          </li>
        </ul>

        <Dialog v-model:open="editDialogOpen">
          <DialogContent class="max-h-[90vh] gap-0 overflow-hidden p-0 sm:max-w-lg">
            <div class="flex max-h-[min(90vh,800px)] flex-col">
              <DialogHeader class="shrink-0 border-b px-6 pt-6 pb-4">
                <DialogTitle>Edit workout</DialogTitle>
                <DialogDescription>
                  Change the label and exercises. Scroll if the list is long.
                </DialogDescription>
              </DialogHeader>
              <div class="min-h-0 flex-1 overflow-y-auto px-6 py-4">
                <div class="flex flex-col gap-4">
                <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
                <div class="space-y-2">
                  <label class="text-sm font-medium" for="dialog-edit-tpl-name">Label</label>
                  <Input id="dialog-edit-tpl-name" v-model="editTemplateName" class="h-11 w-full" />
                </div>
                <div class="space-y-3">
                  <div
                    v-for="(row, idx) in editExercises"
                    :key="`edit-dlg-${idx}`"
                    class="space-y-2 rounded-md border bg-muted/30 p-3"
                  >
                    <div class="flex items-center justify-between gap-2">
                      <span class="text-muted-foreground text-xs font-medium uppercase">
                        Exercise {{ idx + 1 }}
                      </span>
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
                        <label class="text-muted-foreground text-xs">Lbs</label>
                        <Input
                          v-model.number="row.targetWeightLbs"
                          class="h-11 w-full"
                          step="0.5"
                          type="number"
                        />
                      </div>
                    </div>
                  </div>
                </div>
                <Button class="h-10 w-full" type="button" variant="outline" @click="addEditExerciseRow">
                  Add exercise
                </Button>
                </div>
              </div>
              <DialogFooter class="shrink-0 border-t bg-muted/20 px-6 py-4">
                <Button
                  class="h-11 w-full sm:w-auto"
                  type="button"
                  variant="outline"
                  @click="cancelWorkoutEdit"
                >
                  Cancel
                </Button>
                <Button
                  class="h-11 w-full sm:w-auto"
                  type="button"
                  :disabled="savingEdit"
                  @click="saveEditTemplate"
                >
                  {{ savingEdit ? 'Saving…' : 'Save workout' }}
                </Button>
              </DialogFooter>
            </div>
          </DialogContent>
        </Dialog>
      </section>

      <section class="space-y-4 rounded-lg border bg-card p-4 shadow-sm">
        <div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between sm:gap-4">
          <div class="min-w-0 space-y-1">
            <h2 class="text-sm font-medium">Rotating schedule</h2>
            <p class="text-muted-foreground text-sm">
              Sessions per week × weeks that repeat (e.g. ABA / BAB = 3 × 2).
            </p>
          </div>
          <Button
            class="h-11 w-full shrink-0 sm:w-auto"
            type="button"
            :disabled="savingSchedule"
            @click="saveSchedule"
          >
            {{ savingSchedule ? 'Saving…' : 'Save schedule' }}
          </Button>
        </div>
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
            v-for="sl in slotAssignments"
            :key="`${sl.weekIndex}-${sl.sessionIndex}`"
            class="flex flex-col gap-2 rounded-md border bg-background/60 p-3"
          >
            <span class="text-sm font-medium">{{ slotLabel(sl.weekIndex, sl.sessionIndex) }}</span>
            <select
              v-model.number="sl.workoutTemplateId"
              class="border-input bg-background h-11 w-full rounded-md border px-3 text-sm"
            >
              <option v-for="t in templates" :key="t.id" :value="t.id">{{ t.name }}</option>
            </select>
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>
