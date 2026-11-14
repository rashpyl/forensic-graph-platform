<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { usersApi } from '@/services/adminApi'
import { formatApiError } from '@/services/apiError'
import { useAuthStore } from '@/stores/auth'
import { formatDateTimeEU } from '@/composables/useDateTime'
import { USER_ROLES, type UserDto, type UserRole } from '@/types/api'

/**
 * Admin page for user accounts: create, change role, deactivate / reactivate,
 * reset password. Accounts are never deleted so the audit log keeps pointing
 * at real users. The backend refuses to remove the last admin or to let an
 * admin lock themselves out; this page surfaces those refusals as messages.
 */
const { user: me } = storeToRefs(useAuthStore())

const users = ref<UserDto[]>([])
const isLoading = ref(false)
const pageError = ref<string | null>(null)
const rowError = ref<{ id: string; message: string } | null>(null)
const busyId = ref<string | null>(null)

const form = ref({ email: '', displayName: '', role: 'Analyst' as UserRole, password: '' })
const formError = ref<string | null>(null)
const isCreating = ref(false)

const resetFor = ref<string | null>(null)
const resetPassword = ref('')
const resetMessage = ref<string | null>(null)

const activeCount = computed(() => users.value.filter((u) => !u.isLocked).length)

async function load() {
  isLoading.value = true
  pageError.value = null
  try {
    users.value = await usersApi.list()
  } catch (err) {
    pageError.value = formatApiError(err)
  } finally {
    isLoading.value = false
  }
}

function replace(updated: UserDto) {
  users.value = users.value.map((u) => (u.id === updated.id ? updated : u))
}

async function run(id: string, action: () => Promise<UserDto | void>) {
  busyId.value = id
  rowError.value = null
  try {
    const result = await action()
    if (result) replace(result)
  } catch (err) {
    rowError.value = { id, message: formatApiError(err) }
    await load()
  } finally {
    busyId.value = null
  }
}

function changeRole(u: UserDto, role: UserRole) {
  void run(u.id, () => usersApi.changeRole(u.id, role))
}

function toggleActive(u: UserDto) {
  void run(u.id, () => (u.isLocked ? usersApi.activate(u.id) : usersApi.deactivate(u.id)))
}

async function submitReset(u: UserDto) {
  resetMessage.value = null
  if (resetPassword.value.length < 12) {
    rowError.value = { id: u.id, message: 'The new password must be at least 12 characters.' }
    return
  }
  await run(u.id, () => usersApi.resetPassword(u.id, resetPassword.value))
  if (!rowError.value) {
    resetMessage.value = `Password for ${u.email} changed. Give it to the user through a separate channel.`
    resetFor.value = null
    resetPassword.value = ''
  }
}

async function createUser() {
  formError.value = null
  const f = form.value
  if (!f.email.trim() || !f.displayName.trim()) {
    formError.value = 'Enter an e-mail and a display name.'
    return
  }
  if (f.password.length < 12) {
    formError.value = 'The initial password must be at least 12 characters.'
    return
  }

  isCreating.value = true
  try {
    const created = await usersApi.create({ ...f, email: f.email.trim(), displayName: f.displayName.trim() })
    users.value = [...users.value, created].sort((a, b) => a.email.localeCompare(b.email))
    form.value = { email: '', displayName: '', role: 'Analyst', password: '' }
  } catch (err) {
    formError.value = formatApiError(err)
  } finally {
    isCreating.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="admin-page">
    <header class="page-header">
      <h1>Users</h1>
      <span class="muted">{{ activeCount }} active of {{ users.length }}</span>
    </header>

    <section class="card">
      <h2>Add a user</h2>
      <form class="create-form" @submit.prevent="createUser" novalidate>
        <input v-model="form.email" type="email" placeholder="name@agency.example" autocomplete="off" @input="formError = null" />
        <input v-model="form.displayName" type="text" placeholder="Display name" @input="formError = null" />
        <select v-model="form.role">
          <option v-for="r in USER_ROLES" :key="r" :value="r">{{ r }}</option>
        </select>
        <input
          v-model="form.password"
          type="password"
          placeholder="Initial password (12+ characters)"
          autocomplete="new-password"
          @input="formError = null"
        />
        <button class="btn primary" :disabled="isCreating">{{ isCreating ? 'Adding…' : 'Add user' }}</button>
      </form>
      <div v-if="formError" class="form-error">{{ formError }}</div>
      <p class="hint">
        Admin manages accounts and reads the audit log. Analyst edits data and sees passport numbers and phones.
        Viewer reads only, with those fields masked.
      </p>
    </section>

    <div v-if="resetMessage" class="notice">{{ resetMessage }}</div>
    <div v-if="isLoading && !users.length" class="status">Loading users…</div>
    <div v-else-if="pageError" class="form-error">{{ pageError }}</div>

    <div v-if="users.length" class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>User</th>
            <th>Role</th>
            <th>Status</th>
            <th>Created</th>
            <th class="actions-col">Actions</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="u in users" :key="u.id">
            <tr :class="{ locked: u.isLocked }">
              <td>
                <div class="name">{{ u.displayName }} <span v-if="u.id === me?.id" class="you">you</span></div>
                <div class="muted mono">{{ u.email }}</div>
              </td>
              <td>
                <select
                  :value="u.role"
                  :disabled="busyId === u.id"
                  @change="changeRole(u, ($event.target as HTMLSelectElement).value as UserRole)"
                >
                  <option v-for="r in USER_ROLES" :key="r" :value="r">{{ r }}</option>
                </select>
              </td>
              <td>
                <span class="status-pill" :class="u.isLocked ? 'off' : 'on'">
                  {{ u.isLocked ? 'Locked' : 'Active' }}
                </span>
              </td>
              <td class="muted">{{ formatDateTimeEU(u.createdAt) }}</td>
              <td class="actions-col">
                <div class="actions">
                  <button
                    v-if="u.id !== me?.id"
                    class="btn small"
                    :disabled="busyId === u.id"
                    @click="toggleActive(u)"
                  >
                    {{ u.isLocked ? 'Activate' : 'Deactivate' }}
                  </button>
                  <button
                    class="btn small"
                    :disabled="busyId === u.id"
                    @click="resetFor = resetFor === u.id ? null : u.id; resetPassword = ''"
                  >
                    Reset password
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="resetFor === u.id" class="sub-row">
              <td colspan="5">
                <form class="reset-form" @submit.prevent="submitReset(u)">
                  <input
                    v-model="resetPassword"
                    type="password"
                    placeholder="New password (12+ characters)"
                    autocomplete="new-password"
                  />
                  <button class="btn primary small">Set password</button>
                </form>
              </td>
            </tr>
            <tr v-if="rowError?.id === u.id" class="sub-row">
              <td colspan="5"><div class="form-error">{{ rowError.message }}</div></td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.admin-page {
  flex: 1;
  overflow-y: auto;
  padding: 20px 28px 40px;
  color: #e2e8f0;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.page-header {
  display: flex;
  align-items: baseline;
  gap: 12px;
}

.page-header h1 {
  font-size: 1.4rem;
  font-weight: 600;
  color: #f1f5f9;
}

.card {
  background: #1a1d27;
  border: 1px solid #2d3148;
  border-radius: 10px;
  padding: 16px 18px;
}

.card h2 {
  font-size: 0.95rem;
  font-weight: 600;
  margin-bottom: 12px;
  color: #c7d2fe;
}

.create-form,
.reset-form {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.create-form input {
  flex: 1 1 180px;
}

input,
select {
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 7px 10px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.85rem;
}

input:focus,
select:focus {
  outline: none;
  border-color: #6366f1;
}

.hint {
  margin-top: 10px;
  color: #64748b;
  font-size: 0.78rem;
}

.btn {
  background: #232735;
  color: #e2e8f0;
  border: 1px solid #2d3148;
  padding: 7px 12px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.85rem;
  cursor: pointer;
  white-space: nowrap;
}

.btn:hover:not(:disabled) { background: #2d3148; }
.btn:disabled { opacity: 0.55; cursor: not-allowed; }
.btn.small { padding: 4px 10px; font-size: 0.78rem; }
.btn.primary { background: #4f46e5; border-color: #4f46e5; color: #fff; }
.btn.primary:hover:not(:disabled) { background: #4338ca; }

.table-wrap {
  overflow-x: auto;
  border: 1px solid #2d3148;
  border-radius: 10px;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
}

th {
  text-align: left;
  font-weight: 500;
  color: #94a3b8;
  background: #161922;
  padding: 10px 12px;
  border-bottom: 1px solid #2d3148;
}

td {
  padding: 10px 12px;
  border-bottom: 1px solid #232735;
  vertical-align: middle;
}

tr.locked td { opacity: 0.6; }
tr.sub-row td { background: #12141c; padding-top: 6px; }

.actions-col { text-align: right; }

.actions {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
}

.name { color: #f1f5f9; }
.muted { color: #94a3b8; font-size: 0.8rem; }
.mono { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; }

.you {
  font-size: 0.68rem;
  color: #a5b4fc;
  border: 1px solid #3730a3;
  border-radius: 999px;
  padding: 0 6px;
  margin-left: 4px;
}

.status-pill {
  font-size: 0.72rem;
  padding: 2px 8px;
  border-radius: 999px;
}

.status-pill.on { background: #052e16; color: #bbf7d0; border: 1px solid #166534; }
.status-pill.off { background: #3b1219; color: #fecaca; border: 1px solid #7f1d1d; }

.form-error {
  margin-top: 8px;
  background: #3b1219;
  border: 1px solid #7f1d1d;
  color: #fecaca;
  font-size: 0.82rem;
  padding: 8px 10px;
  border-radius: 6px;
}

.notice {
  background: #052e16;
  border: 1px solid #166534;
  color: #bbf7d0;
  font-size: 0.85rem;
  padding: 8px 12px;
  border-radius: 6px;
}

.status { color: #94a3b8; }
</style>
