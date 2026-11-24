<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useUiStore } from '@/stores/ui'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/services/authApi'
import { formatApiError } from '@/services/apiError'

/**
 * Lets the signed-in user replace the password an Admin gave them, so the
 * Admin no longer knows it. The backend checks the current password and the
 * password policy; the checks here only save a round trip.
 */
const uiStore = useUiStore()
const authStore = useAuthStore()
const { isChangePasswordDialogOpen } = storeToRefs(uiStore)
const { isAuthenticated } = storeToRefs(authStore)

const MinLength = 12

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const submitting = ref(false)
const error = ref<string | null>(null)
const done = ref(false)

const isOpen = computed(() => isChangePasswordDialogOpen.value && isAuthenticated.value)

function reset() {
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
  submitting.value = false
  error.value = null
  done.value = false
}

watch(isChangePasswordDialogOpen, (open) => {
  if (open) reset()
})

// The session can end while the dialog is open; don't leave it pending for the next login.
watch(isAuthenticated, (signedIn) => {
  if (!signedIn) uiStore.closeChangePasswordDialog()
})

function close() {
  uiStore.closeChangePasswordDialog()
}

async function submit() {
  error.value = null
  if (!currentPassword.value) {
    error.value = 'Enter your current password.'
    return
  }
  if (newPassword.value.length < MinLength) {
    error.value = `The new password must be at least ${MinLength} characters.`
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = 'The new passwords do not match.'
    return
  }

  submitting.value = true
  try {
    await authApi.changePassword(currentPassword.value, newPassword.value)
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
    done.value = true
  } catch (err) {
    error.value = formatApiError(err)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div v-if="isOpen" class="dialog-backdrop" @click.self="close">
    <form class="dialog" role="dialog" aria-labelledby="change-password-title" @submit.prevent="submit">
      <header class="dialog-header">
        <h2 id="change-password-title">Change password</h2>
        <button type="button" class="icon-btn" @click="close" aria-label="Close">✕</button>
      </header>

      <div class="dialog-body">
        <template v-if="!done">
          <label class="field">
            <span class="field-label">Current password</span>
            <input v-model="currentPassword" type="password" autocomplete="current-password" required />
          </label>
          <label class="field">
            <span class="field-label">New password ({{ MinLength }}+ characters)</span>
            <input v-model="newPassword" type="password" autocomplete="new-password" required />
          </label>
          <label class="field">
            <span class="field-label">Repeat new password</span>
            <input v-model="confirmPassword" type="password" autocomplete="new-password" required />
          </label>

          <div v-if="error" class="form-error">{{ error }}</div>
        </template>
        <div v-else class="form-success">Your password has been changed.</div>
      </div>

      <footer class="dialog-footer">
        <template v-if="!done">
          <button type="button" class="btn ghost" @click="close" :disabled="submitting">Cancel</button>
          <button type="submit" class="btn primary" :disabled="submitting">
            {{ submitting ? 'Saving…' : 'Change password' }}
          </button>
        </template>
        <button v-else type="button" class="btn primary" @click="close">Close</button>
      </footer>
    </form>
  </div>
</template>

<style scoped>
.dialog-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.dialog {
  width: min(420px, 92vw);
  background: #1a1d27;
  border: 1px solid #2d3148;
  border-radius: 10px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.6);
  display: flex;
  flex-direction: column;
  color: #e2e8f0;
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #2d3148;
}

.dialog-header h2 {
  font-size: 1rem;
  font-weight: 600;
  color: #a5b4fc;
  letter-spacing: 0.02em;
}

.icon-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  font-size: 1rem;
  padding: 4px 8px;
  border-radius: 4px;
}

.icon-btn:hover { background: #2d3148; color: #e2e8f0; }

.dialog-body {
  padding: 16px 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.field-label {
  font-size: 0.8rem;
  color: #94a3b8;
  letter-spacing: 0.03em;
}

.field input {
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 8px 10px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.9rem;
}

.field input:focus {
  outline: none;
  border-color: #6366f1;
}

.form-error {
  background: rgba(153, 27, 27, 0.25);
  border: 1px solid #ef4444;
  color: #fecaca;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: 0.85rem;
  white-space: pre-line;
}

.form-success {
  background: rgba(22, 101, 52, 0.25);
  border: 1px solid #22c55e;
  color: #bbf7d0;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: 0.85rem;
}

.dialog-footer {
  padding: 12px 20px;
  border-top: 1px solid #2d3148;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.btn {
  padding: 8px 14px;
  border-radius: 6px;
  border: 1px solid transparent;
  font-size: 0.85rem;
  cursor: pointer;
}

.btn.primary { background: #6366f1; color: white; }
.btn.primary:hover:not(:disabled) { background: #4f46e5; }
.btn.ghost {
  background: transparent;
  border-color: #2d3148;
  color: #cbd5e1;
}
.btn.ghost:hover:not(:disabled) { background: #232735; }
.btn:disabled { opacity: 0.6; cursor: not-allowed; }
</style>
