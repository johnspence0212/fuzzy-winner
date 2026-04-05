<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

import { runRequest } from '@/api/base/client'
import { planApi } from '@/api/planApi'
import type { Plan } from '@/api/types/schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const router = useRouter()
const plans = ref<Plan[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const newName = ref('')
const newNotes = ref('')

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

const createPlan = async () => {
  const name = newName.value.trim()
  if (!name) {
    error.value = 'Enter a plan name.'
    return
  }
  error.value = null
  try {
    const created = await runRequest(
      planApi.create({
        name,
        notes: newNotes.value.trim() ? newNotes.value.trim() : null,
      } as Omit<Plan, 'id' | 'createdAt'>),
    )
    newName.value = ''
    newNotes.value = ''
    await router.push({ name: 'plan-detail', params: { planId: String(created.id) } })
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Could not create plan.'
  }
}
</script>

<template>
  <div class="mx-auto flex w-full max-w-lg flex-col gap-6 p-4 pb-24">
    <header class="space-y-1">
      <h1 class="text-2xl font-semibold tracking-tight">Plans</h1>
      <p class="text-muted-foreground text-sm">
        Build programs, workouts, and your rotating schedule here.
      </p>
    </header>

    <section class="space-y-3 rounded-lg border bg-card p-4 shadow-sm">
      <h2 class="text-sm font-medium">New plan</h2>
      <div class="space-y-3">
        <div class="space-y-1">
          <label class="text-sm font-medium" for="plan-name">Name</label>
          <Input
            id="plan-name"
            v-model="newName"
            class="h-11 w-full"
            type="text"
            autocomplete="off"
            placeholder="e.g. Off-season strength"
          />
        </div>
        <div class="space-y-1">
          <label class="text-sm font-medium" for="plan-notes">Notes (optional)</label>
          <Input
            id="plan-notes"
            v-model="newNotes"
            class="h-11 w-full"
            type="text"
            autocomplete="off"
            placeholder="Short reminder for yourself"
          />
        </div>
        <Button class="h-11 w-full" type="button" @click="createPlan">Create plan</Button>
      </div>
    </section>

    <p v-if="error" class="text-destructive text-sm">{{ error }}</p>

    <section v-if="loading" class="text-muted-foreground text-sm">Loading…</section>

    <ul v-else class="flex flex-col gap-3">
      <li
        v-for="p in plans"
        :key="p.id"
        class="rounded-lg border bg-card shadow-sm transition-colors active:bg-accent/40"
      >
        <router-link
          class="flex flex-col gap-1 p-4"
          :to="{ name: 'plan-detail', params: { planId: String(p.id) } }"
        >
          <span class="font-medium">{{ p.name }}</span>
          <span v-if="p.notes" class="text-muted-foreground line-clamp-2 text-sm">{{ p.notes }}</span>
          <span v-else class="text-muted-foreground text-sm">Tap to edit templates & schedule</span>
        </router-link>
      </li>
      <li v-if="!plans.length" class="text-muted-foreground rounded-lg border border-dashed p-6 text-center text-sm">
        No plans yet. Create one above.
      </li>
    </ul>
  </div>
</template>
