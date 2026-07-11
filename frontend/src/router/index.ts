import { createRouter, createWebHistory } from 'vue-router'

import ActiveWorkoutView from '@/views/ActiveWorkoutView.vue'
import PlanDetailView from '@/views/PlanDetailView.vue'
import PlanListView from '@/views/PlanListView.vue'
import WorkoutView from '@/views/WorkoutView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/workout',
    },
    {
      path: '/plan',
      name: 'plan',
      component: PlanListView,
    },
    {
      path: '/plan/:planId',
      name: 'plan-detail',
      component: PlanDetailView,
      meta: {
        breadcrumbs: [
          { label: 'Plan', path: '/plan' },
          { label: 'Setup', path: '' },
        ],
      },
    },
    {
      path: '/workout',
      name: 'workout',
      component: WorkoutView,
    },
    {
      path: '/workout/active',
      name: 'workout-active',
      component: ActiveWorkoutView,
      meta: {
        isolated: true,
      },
    },
  ],
})

export default router
