<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import { formatApiError } from '@/services/apiError'

/**
 * Sign-in page. Shows one generic message for any failed attempt — the API
 * deliberately does not reveal whether the e-mail exists or the account is
 * locked — and a separate one when the per-IP rate limit is hit.
 */
const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const isSubmitting = ref(false)
const error = ref<string | null>(null)

function safeRedirect(target: unknown): string {
  // Only follow in-app paths, never an absolute URL (open-redirect protection).
  return typeof target === 'string' && target.startsWith('/') && !target.startsWith('//') ? target : '/'
}

async function submit() {
  error.value = null
  if (!email.value.trim() || !password.value) {
    error.value = 'Enter your e-mail and password.'
    return
  }

  isSubmitting.value = true
  try {
    await auth.login(email.value.trim(), password.value)
    password.value = ''
    await router.replace(safeRedirect(route.query.redirect))
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 429) {
      error.value = 'Too many sign-in attempts from this device. Wait a minute and try again.'
    } else if (axios.isAxiosError(err) && err.response?.status === 401) {
      error.value = 'Invalid e-mail or password.'
    } else {
      error.value = formatApiError(err)
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-card" @submit.prevent="submit" novalidate>
      <div class="brand">
        <span class="brand-icon" aria-hidden="true">🔍</span>
        <h1>Forensic Graph Platform</h1>
      </div>
      <p class="subtitle">Sign in with the account your administrator created for you.</p>

      <label class="field">
        <span>E-mail</span>
        <input
          v-model="email"
          type="email"
          autocomplete="username"
          placeholder="name@agency.example"
          autofocus
          @input="error = null"
        />
      </label>

      <label class="field">
        <span>Password</span>
        <input
          v-model="password"
          type="password"
          autocomplete="current-password"
          @input="error = null"
        />
      </label>

      <div v-if="error" class="form-error" role="alert">{{ error }}</div>

      <button type="submit" class="btn primary" :disabled="isSubmitting">
        {{ isSubmitting ? 'Signing in…' : 'Sign in' }}
      </button>

      <p class="note">
        Access is logged. After 5 failed attempts the account is locked for 15 minutes.
      </p>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px 16px;
}

.login-card {
  width: 100%;
  max-width: 380px;
  background: #1a1d27;
  border: 1px solid #2d3148;
  border-radius: 12px;
  padding: 28px 28px 22px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
}

.brand h1 {
  font-size: 1.15rem;
  font-weight: 600;
  color: #c7d2fe;
}

.subtitle {
  color: #94a3b8;
  font-size: 0.85rem;
  margin-bottom: 4px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 0.8rem;
  color: #94a3b8;
}

.field input {
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 9px 12px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.92rem;
}

.field input:focus {
  outline: none;
  border-color: #6366f1;
}

.form-error {
  background: #3b1219;
  border: 1px solid #7f1d1d;
  color: #fecaca;
  font-size: 0.83rem;
  padding: 8px 10px;
  border-radius: 6px;
}

.btn {
  border: 1px solid #2d3148;
  padding: 9px 14px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.9rem;
  cursor: pointer;
}

.btn.primary {
  background: #4f46e5;
  border-color: #4f46e5;
  color: #fff;
}

.btn.primary:hover:not(:disabled) {
  background: #4338ca;
}

.btn:disabled {
  opacity: 0.6;
  cursor: wait;
}

.note {
  color: #64748b;
  font-size: 0.75rem;
  text-align: center;
}
</style>
