<script setup lang="ts">
import { ClipboardList, Dumbbell } from 'lucide-vue-next'
import { computed } from 'vue'
import { useRoute } from 'vue-router'

import AppSidebar from './components/AppSidebar.vue'
import { Separator } from '@/components/ui/separator'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'

const route = useRoute()

const routeInfo: Record<string, { label: string; icon: typeof Dumbbell }> = {
  plan: { label: 'Plan', icon: ClipboardList },
  'plan-detail': { label: 'Plan setup', icon: ClipboardList },
  workout: { label: 'Workout', icon: Dumbbell },
}

const currentRoute = computed(() => {
  const name = route.name as string
  return routeInfo[name] ?? { label: 'Workout', icon: Dumbbell }
})

const breadcrumbs = computed(() => {
  if (route.name === 'plan-detail' && route.meta.breadcrumbs) {
    return route.meta.breadcrumbs as Array<{ label: string; path: string }>
  }
  const name = route.name as string
  if (routeInfo[name]) {
    return [{ label: routeInfo[name].label, path: route.path }]
  }
  return [{ label: 'Workout', path: '/workout' }]
})
</script>

<template>
  <SidebarProvider>
    <AppSidebar />
    <SidebarInset class="min-h-0 min-h-svh">
      <header class="flex h-14 shrink-0 items-center gap-2 border-b px-4">
        <SidebarTrigger class="-ml-1" />
        <Separator class="mr-2 h-4" orientation="vertical" />
        <nav class="text-muted-foreground flex flex-wrap items-center gap-2 text-sm">
          <component :is="currentRoute.icon" class="text-foreground h-4 w-4" />
          <template v-for="(crumb, index) in breadcrumbs" :key="`${crumb.path}-${index}`">
            <router-link
              v-if="index < breadcrumbs.length - 1 && crumb.path"
              class="text-foreground font-medium hover:underline"
              :to="crumb.path"
            >
              {{ crumb.label }}
            </router-link>
            <span v-else class="text-foreground font-medium">{{ crumb.label }}</span>
            <span v-if="index < breadcrumbs.length - 1">/</span>
          </template>
        </nav>
      </header>
      <div class="flex min-h-0 min-w-0 flex-1 flex-col overflow-x-hidden overflow-y-auto overscroll-y-contain">
        <router-view />
      </div>
    </SidebarInset>
  </SidebarProvider>
</template>
