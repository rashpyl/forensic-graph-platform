import { createRouter, createWebHistory } from 'vue-router'
import MapView from '@/views/MapView.vue'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    /** Reachable without signing in. */
    public?: boolean
    /** Only for the Admin role. */
    adminOnly?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/',
      name: 'map',
      component: MapView,
    },
    {
      path: '/persons',
      name: 'persons',
      component: () => import('@/views/PersonsView.vue'),
    },
    {
      path: '/admin/users',
      name: 'admin-users',
      component: () => import('@/views/admin/UsersView.vue'),
      meta: { adminOnly: true },
    },
    {
      path: '/admin/audit',
      name: 'admin-audit',
      component: () => import('@/views/admin/AuditView.vue'),
      meta: { adminOnly: true },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

/**
 * Sends signed-out users to the login page (remembering where they were going)
 * and keeps non-admins out of admin pages. This only shapes navigation; the
 * backend independently rejects every unauthorized request.
 */
router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.init()

  if (to.meta.public) {
    return auth.isAuthenticated && to.name === 'login' ? { name: 'map' } : true
  }

  if (!auth.isAuthenticated) {
    return { name: 'login', query: to.fullPath !== '/' ? { redirect: to.fullPath } : {} }
  }

  if (to.meta.adminOnly && !auth.isAdmin) {
    return { name: 'map' }
  }

  return true
})

export default router
