import { createRouter, createWebHistory } from 'vue-router'
import MapView from '@/views/MapView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
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
      // Kept for reference; the Cytoscape prototype is not linked from the
      // navbar but remains reachable at /graph while we iterate on the map UI.
      path: '/graph',
      name: 'graph-editor',
      component: () => import('@/views/GraphEditorView.vue'),
    },
  ],
})

export default router
