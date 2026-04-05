<script setup lang="ts">
import { watch } from 'vue'
import { useRoute } from 'vue-router'
import { ClipboardList, Dumbbell } from 'lucide-vue-next'
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from '@/components/ui/sidebar'

const route = useRoute()
const { isMobile, setOpenMobile } = useSidebar()

/** Close the mobile drawer after any navigation (breadcrumbs, programmatic, etc.). */
watch(
  () => route.fullPath,
  () => {
    if (isMobile.value) setOpenMobile(false)
  },
)

/** Close on sidebar tap even when the route is unchanged (same section). */
function closeMobileDrawer() {
  if (isMobile.value) setOpenMobile(false)
}

const items = [
  {
    title: 'Workout',
    url: '/workout',
    icon: Dumbbell,
  },
  {
    title: 'Plan',
    url: '/plan',
    icon: ClipboardList,
  },
]
</script>

<template>
  <Sidebar>
    <SidebarContent>
      <SidebarGroup>
        <SidebarGroupLabel>Training</SidebarGroupLabel>
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem v-for="item in items" :key="item.title">
              <SidebarMenuButton as-child>
                <router-link :to="item.url" @click="closeMobileDrawer">
                  <component :is="item.icon" />
                  <span>{{ item.title }}</span>
                </router-link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>
    </SidebarContent>
  </Sidebar>
</template>
