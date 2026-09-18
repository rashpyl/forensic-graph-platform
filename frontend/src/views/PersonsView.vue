<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'

import { usePersonsStore } from '@/stores/persons'
import { useUiStore } from '@/stores/ui'
import { formatApiError } from '@/services/apiError'
import type { PersonDto } from '@/types/api'

/**
 * Secondary page listing every known person, with a client-side search box.
 * The map is the primary UX; this page exists so the user can browse and
 * groom the person catalogue without going through an event assignment.
 */

const personsStore = usePersonsStore()
const uiStore = useUiStore()
const { persons, isLoading, error } = storeToRefs(personsStore)

const query = ref('')
const deleteError = ref<string | null>(null)

const filtered = computed<PersonDto[]>(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return persons.value
  return persons.value.filter((p) => {
    const full = `${p.firstName} ${p.lastName}`.toLowerCase()
    return (
      full.includes(q) ||
      p.citizenships.some((c) => c.toLowerCase().includes(q)) ||
      p.passportNumbers.some((n) => n.toLowerCase().includes(q)) ||
      (p.phone?.toLowerCase().includes(q) ?? false)
    )
  })
})

onMounted(async () => {
  try {
    await personsStore.fetchAll()
  } catch {
    // error state surfaces via the store.
  }
})

async function remove(p: PersonDto) {
  const ok = window.confirm(
    `Delete "${p.firstName} ${p.lastName}"?\n\nAny event assignments referring to this person will be removed by cascade.`,
  )
  if (!ok) return
  deleteError.value = null
  try {
    await personsStore.remove(p.id)
  } catch (err) {
    deleteError.value = formatApiError(err)
  }
}
</script>

<template>
  <div class="persons-view">
    <header class="page-header">
      <h1>Persons</h1>
      <button class="btn primary" @click="uiStore.openNewPersonDialog()">
        <span class="btn-icon">＋</span> New Person
      </button>
    </header>

    <div class="search-row">
      <input
        v-model="query"
        type="search"
        placeholder="Filter by name, citizenship, passport, or phone…"
        class="search-input"
      />
      <span class="result-count">
        {{ filtered.length }} of {{ persons.length }}
      </span>
    </div>

    <div v-if="isLoading" class="status">Loading persons…</div>
    <div v-else-if="error" class="status error">Could not load persons: {{ error }}</div>

    <div v-if="deleteError" class="status error">{{ deleteError }}</div>

    <div v-if="!isLoading && !error" class="table-wrap">
      <table class="persons-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Citizenships</th>
            <th>Passports</th>
            <th>Phone</th>
            <th class="actions-col">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in filtered" :key="p.id">
            <td class="name-cell">
              <span class="full-name">{{ p.firstName }} {{ p.lastName }}</span>
              <span v-if="p.physicalDescription" class="physical">{{ p.physicalDescription }}</span>
            </td>
            <td>
              <span v-for="c in p.citizenships" :key="c" class="chip">{{ c }}</span>
              <span v-if="!p.citizenships.length" class="empty">—</span>
            </td>
            <td>
              <span v-for="n in p.passportNumbers" :key="n" class="chip mono">{{ n }}</span>
              <span v-if="!p.passportNumbers.length" class="empty">—</span>
            </td>
            <td class="mono">{{ p.phone || '—' }}</td>
            <td class="actions-col">
              <button class="icon-btn danger" @click="remove(p)" title="Delete person">✕</button>
            </td>
          </tr>
          <tr v-if="!filtered.length">
            <td colspan="5" class="empty-row">No persons match the current filter.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.persons-view {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 20px 28px;
  overflow-y: auto;
  color: #e2e8f0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.page-header h1 {
  font-size: 1.4rem;
  color: #f1f5f9;
  font-weight: 600;
}

.search-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.search-input {
  flex: 1;
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 8px 12px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.9rem;
}

.search-input:focus {
  outline: none;
  border-color: #6366f1;
}

.result-count {
  color: #94a3b8;
  font-size: 0.85rem;
}

.status {
  padding: 10px 14px;
  border-radius: 6px;
  background: #1a1d27;
  border: 1px solid #2d3148;
  color: #cbd5e1;
  font-size: 0.9rem;
  margin-bottom: 12px;
}

.status.error {
  background: rgba(153, 27, 27, 0.25);
  border-color: #ef4444;
  color: #fecaca;
}

.table-wrap {
  overflow-x: auto;
  border: 1px solid #2d3148;
  border-radius: 8px;
}

.persons-table {
  width: 100%;
  border-collapse: collapse;
  background: #1a1d27;
}

.persons-table th,
.persons-table td {
  text-align: left;
  padding: 10px 14px;
  border-bottom: 1px solid #2d3148;
  font-size: 0.85rem;
  vertical-align: top;
}

.persons-table th {
  background: #232735;
  color: #a5b4fc;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-size: 0.75rem;
  font-weight: 600;
}

.persons-table tbody tr:last-child td {
  border-bottom: none;
}

.persons-table tbody tr:hover {
  background: #232735;
}

.name-cell {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.full-name {
  font-weight: 600;
  color: #f1f5f9;
}

.physical {
  color: #94a3b8;
  font-size: 0.75rem;
  line-height: 1.3;
}

.chip {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  background: #312e81;
  color: #c7d2fe;
  font-size: 0.75rem;
  margin: 2px 4px 2px 0;
}

.chip.mono {
  background: #1e40af;
  color: #bfdbfe;
  font-family: 'SF Mono', Menlo, monospace;
}

.mono { font-family: 'SF Mono', Menlo, monospace; font-size: 0.8rem; }

.empty { color: #64748b; font-style: italic; }
.empty-row {
  text-align: center;
  color: #94a3b8;
  font-style: italic;
  padding: 24px !important;
}

.actions-col { width: 60px; text-align: center; }

.icon-btn {
  background: transparent;
  border: 1px solid #2d3148;
  color: #94a3b8;
  cursor: pointer;
  width: 26px;
  height: 26px;
  border-radius: 4px;
  font-size: 0.75rem;
}

.icon-btn.danger:hover {
  background: #7f1d1d;
  color: #fecaca;
  border-color: #ef4444;
}

.btn {
  padding: 8px 14px;
  border-radius: 6px;
  border: 1px solid transparent;
  font-size: 0.85rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.btn.primary { background: #6366f1; color: white; }
.btn.primary:hover { background: #4f46e5; }
.btn-icon { font-size: 0.95rem; line-height: 1; }
</style>
