import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import axios from 'axios'
import { authApi } from '@/services/authApi'
import type { CurrentUserDto } from '@/types/api'

/**
 * The signed-in user and what they may do.
 *
 * The UI hides actions a role cannot perform, but this is only for usability:
 * every permission is enforced again by the backend, which is the actual
 * security boundary.
 */
export const useAuthStore = defineStore('auth', () => {
  const user = ref<CurrentUserDto | null>(null)

  const isAuthenticated = computed(() => user.value !== null)
  const role = computed(() => user.value?.role ?? null)
  const isAdmin = computed(() => role.value === 'Admin')
  const canEdit = computed(() => role.value === 'Admin' || role.value === 'Analyst')

  let initPromise: Promise<void> | null = null

  /** Restores the session from the cookie, once per page load. */
  function init(): Promise<void> {
    if (!initPromise) {
      initPromise = (async () => {
        try {
          user.value = await authApi.me()
        } catch (err) {
          if (!axios.isAxiosError(err) || err.response?.status !== 401) {
            console.error('Could not restore session', err)
          }
          user.value = null
        }
      })()
    }
    return initPromise
  }

  async function login(email: string, password: string): Promise<void> {
    user.value = await authApi.login(email, password)
  }

  async function logout(): Promise<void> {
    try {
      await authApi.logout()
    } finally {
      user.value = null
    }
  }

  /** Called when any request comes back 401: the session expired or was revoked. */
  function sessionExpired(): void {
    user.value = null
  }

  return {
    user,
    isAuthenticated,
    role,
    isAdmin,
    canEdit,
    init,
    login,
    logout,
    sessionExpired,
  }
})
