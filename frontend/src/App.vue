<script setup lang="ts">
import { computed, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useUiStore } from '@/stores/ui'
import { useAuthStore } from '@/stores/auth'
import { onUnauthorized } from '@/services/apiClient'
import NewPersonDialog from '@/components/NewPersonDialog.vue'
import AboutModal from '@/components/AboutModal.vue'

const uiStore = useUiStore()
const authStore = useAuthStore()
const { user, isAuthenticated, canEdit, isAdmin } = storeToRefs(authStore)
const route = useRoute()
const router = useRouter()

const isMapRoute = computed(() => route.name === 'map')
const showChrome = computed(() => isAuthenticated.value && !route.meta.public)

// Any 401 from the API (session expired, account deactivated) returns to login.
const stopListening = onUnauthorized(() => {
  authStore.sessionExpired()
  if (!route.meta.public) {
    void router.replace({ name: 'login', query: { redirect: route.fullPath } })
  }
})
onBeforeUnmount(stopListening)

async function signOut() {
  await authStore.logout()
  await router.replace({ name: 'login' })
}

function startNewEvent() {
  uiStore.startPickingLocation()
}
</script>

<template>
  <div id="app-layout">
    <header v-if="showChrome" class="topbar">
      <span class="topbar-title">🔍 Forensic Graph Platform</span>
      <nav class="topbar-links">
        <RouterLink to="/" class="nav-link" :class="{ active: isMapRoute }">Map</RouterLink>
        <RouterLink to="/persons" class="nav-link" active-class="active">Persons</RouterLink>
        <RouterLink v-if="isAdmin" to="/admin/users" class="nav-link" active-class="active">Users</RouterLink>
        <RouterLink v-if="isAdmin" to="/admin/audit" class="nav-link" active-class="active">Audit log</RouterLink>
      </nav>
      <nav class="topbar-actions">
        <button
          v-if="canEdit"
          class="nav-btn"
          :disabled="!isMapRoute"
          :title="isMapRoute ? 'Pick a spot on the map' : 'Switch to the map to place a new event'"
          @click="startNewEvent"
        >
          <span class="nav-icon">＋</span> New Event
        </button>
        <button v-if="canEdit" class="nav-btn" @click="uiStore.openNewPersonDialog()">
          <span class="nav-icon">👤</span> New Person
        </button>
        <button class="nav-btn" @click="uiStore.openAboutModal()">
          <span class="nav-icon">ℹ</span> About
        </button>
        <div v-if="user" class="user-chip" :title="user.email">
          <span class="user-name">{{ user.displayName }}</span>
          <span class="role-pill" :data-role="user.role">{{ user.role }}</span>
          <button class="nav-btn subtle" @click="signOut">Sign out</button>
        </div>
      </nav>
    </header>
    <main class="main-content">
      <RouterView />
    </main>

    <NewPersonDialog />
    <AboutModal />
  </div>
</template>

<style>
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
  font-family: 'Segoe UI', system-ui, sans-serif;
  background: #0f1117;
  color: #e2e8f0;
  height: 100vh;
  overflow: hidden;
}

#app-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.topbar {
  height: 52px;
  background: #1a1d27;
  border-bottom: 1px solid #2d3148;
  display: flex;
  align-items: center;
  padding: 0 20px;
  flex-shrink: 0;
  gap: 24px;
}

.topbar-title {
  font-size: 1rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: #a5b4fc;
}

.topbar-links {
  display: flex;
  gap: 4px;
}

.nav-link {
  color: #94a3b8;
  text-decoration: none;
  font-size: 0.85rem;
  padding: 6px 12px;
  border-radius: 6px;
  transition: background 0.15s, color 0.15s;
}

.nav-link:hover { background: #232735; color: #e2e8f0; }
.nav-link.active { background: #312e81; color: #c7d2fe; }

.topbar-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.nav-btn {
  background: #232735;
  color: #e2e8f0;
  border: 1px solid #2d3148;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: background 0.15s;
}

.nav-btn:hover:not(:disabled) {
  background: #2d3148;
}

.nav-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.nav-icon {
  font-size: 0.95rem;
  line-height: 1;
}

.user-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-left: 12px;
  margin-left: 4px;
  border-left: 1px solid #2d3148;
}

.user-name {
  font-size: 0.85rem;
  color: #cbd5e1;
}

.role-pill {
  font-size: 0.7rem;
  padding: 2px 8px;
  border-radius: 999px;
  background: #1e293b;
  color: #cbd5e1;
  border: 1px solid #334155;
}

.role-pill[data-role='Admin'] { background: #3b0764; color: #e9d5ff; border-color: #6b21a8; }
.role-pill[data-role='Analyst'] { background: #172554; color: #bfdbfe; border-color: #1e40af; }
.role-pill[data-role='Viewer'] { background: #1f2937; color: #d1d5db; border-color: #4b5563; }

.nav-btn.subtle {
  background: transparent;
}

.main-content {
  flex: 1;
  overflow: hidden;
  display: flex;
}
</style>
