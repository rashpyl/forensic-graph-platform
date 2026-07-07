import { createRouter, createWebHistory } from 'vue-router'
import GraphEditorView from '../views/GraphEditorView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'graph-editor',
      component: GraphEditorView,
    },
  ],
})

export default router
