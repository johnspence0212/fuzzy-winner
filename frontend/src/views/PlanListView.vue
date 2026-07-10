<script setup lang="ts">
import { Plus } from 'lucide-vue-next'
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import { runRequest } from '@/api/base/client'
import { planApi } from '@/api/planApi'
import type { Plan } from '@/api/types/schema'
import { Button } from '@/components/ui/button'
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

const router = useRouter()
const plans = ref<Plan[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const createOpen = ref(false)
const newName = ref('')
const newNotes = ref('')
const creating = ref(false)

const load = async () => {
  loading.value = true
  error.value = null
  try {
    const list = await runRequest(planApi.getAll())
    plans.value = [...list]
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load plans.'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  void load()
})

/** Clear draft when closing the sheet */
watch(createOpen, (open) => {
  if (!open) {
    newName.value = ''
    newNotes.value = ''
    error.value = null
  }
})

const createPlan = async () => {
  const name = newName.value.trim()
  if (!name) {
    error.value = 'Enter a plan name.'
    return
  }
  error.value = null
  creating.value = true
  try {
    const created = await runRequest(
      planApi.create({
        name,
        notes: newNotes.value.trim() ? newNotes.value.trim() : null,
      } as Omit<Plan, 'id' | 'createdAt'>),
    )
    createOpen.value = false
    await router.push({ name: 'plan-detail', params: { planId: String(created.id) } })
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not create plan.'
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <div class="relative flex w-full flex-col gap-6 p-4 pb-28 sm:p-6 lg:px-8 lg:py-6">
    <header class="space-y-1">
      <h1 class="text-2xl font-semibold tracking-tight sm:text-3xl">Plans</h1>
      <p class="text-muted-foreground text-sm">
        Build programs, workouts, and your rotating schedule here.
      </p>
    </header>

    <p v-if="error && !createOpen" class="text-destructive text-sm">{{ error }}</p>

    <section v-if="loading" class="text-muted-foreground text-sm">Loading…</section>

    <ul v-else class="grid grid-cols-1 gap-3 sm:grid-cols-[repeat(auto-fit,minmax(280px,1fr))]">
      <li
        v-for="p in plans"
        :key="p.id"
        class="rounded-xl border bg-card shadow-sm transition-colors active:bg-accent/40"
      >
        <router-link
          class="flex h-full flex-col gap-1 p-4 sm:p-5"
          :to="{ name: 'plan-detail', params: { planId: String(p.id) } }"
        >
          <span class="font-medium">{{ p.name }}</span>
          <span v-if="p.notes" class="text-muted-foreground line-clamp-2 text-sm">{{ p.notes }}</span>
          <span v-else class="text-muted-foreground text-sm">Tap to edit templates & schedule</span>
        </router-link>
      </li>
      <li
        v-if="!plans.length"
        class="text-muted-foreground rounded-xl border border-dashed p-8 text-center text-sm sm:col-span-full"
      >
        No plans yet. Tap the + button to create one.
      </li>
    </ul>

    <Sheet v-model:open="createOpen">
      <SheetTrigger as-child>
        <Button
          class="fixed right-4 bottom-6 z-40 h-14 w-14 shrink-0 rounded-full shadow-lg md:bottom-8 md:right-8"
          size="icon"
          type="button"
          aria-label="New plan"
        >
          <Plus class="size-7" />
        </Button>
      </SheetTrigger>
      <SheetContent class="rounded-t-2xl px-4 pb-8" side="bottom">
        <SheetHeader class="text-left">
          <SheetTitle>New plan</SheetTitle>
          <SheetDescription>Name your program. You can add workouts and schedule next.</SheetDescription>
        </SheetHeader>
        <div class="flex flex-col gap-4 py-2">
          <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
          <div class="space-y-1">
            <label class="text-sm font-medium" for="sheet-plan-name">Name</label>
            <Input
              id="sheet-plan-name"
              v-model="newName"
              class="h-11 w-full"
              type="text"
              autocomplete="off"
              placeholder="e.g. Off-season strength"
            />
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium" for="sheet-plan-notes">Notes (optional)</label>
            <Input
              id="sheet-plan-notes"
              v-model="newNotes"
              class="h-11 w-full"
              type="text"
              autocomplete="off"
              placeholder="Short reminder for yourself"
            />
          </div>
        </div>
        <SheetFooter class="mt-2 flex-col gap-2 sm:flex-col">
          <Button
            class="h-11 w-full"
            type="button"
            :disabled="creating"
            @click="createPlan"
          >
            {{ creating ? 'Creating…' : 'Create plan' }}
          </Button>
          <Button class="h-11 w-full" type="button" variant="outline" @click="createOpen = false">
            Cancel
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  </div>
</template>
