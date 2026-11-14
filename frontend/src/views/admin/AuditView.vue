<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { auditApi } from '@/services/adminApi'
import { formatApiError } from '@/services/apiError'
import { formatDateTimeEU } from '@/composables/useDateTime'
import type { AuditEntryDto } from '@/types/api'

/**
 * Read-only view of the append-only audit log: who changed what and when,
 * with old and new values. Sensitive values arrive already masked from the server.
 */
const PAGE_SIZE = 50
const ENTITY_TYPES = ['', 'CrimeEvent', 'Person', 'EventPerson', 'EventLink', 'User', 'UserRole']

const entries = ref<AuditEntryDto[]>([])
const total = ref(0)
const page = ref(1)
const entityType = ref('')
const entityId = ref('')
const isLoading = ref(false)
const error = ref<string | null>(null)
const expanded = ref<Set<number>>(new Set())

const pageCount = computed(() => Math.max(1, Math.ceil(total.value / PAGE_SIZE)))

type Change = { field: string; old: unknown; new: unknown; value?: unknown }

function parseChanges(raw: string): Change[] {
  try {
    const obj = JSON.parse(raw) as Record<string, { old?: unknown; new?: unknown; value?: unknown }>
    return Object.entries(obj).map(([field, v]) => ({ field, old: v.old, new: v.new, value: v.value }))
  } catch {
    return []
  }
}

function show(value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (Array.isArray(value)) return value.length ? value.join(', ') : '—'
  return String(value)
}

function summary(entry: AuditEntryDto): string {
  const fields = parseChanges(entry.changes).map((c) => c.field)
  if (entry.action !== 'Updated') return `${fields.length} field(s)`
  return fields.join(', ')
}

function toggle(id: number) {
  const next = new Set(expanded.value)
  if (next.has(id)) next.delete(id)
  else next.add(id)
  expanded.value = next
}

async function load() {
  isLoading.value = true
  error.value = null
  try {
    const result = await auditApi.list({
      page: page.value,
      pageSize: PAGE_SIZE,
      entityType: entityType.value || undefined,
      entityId: entityId.value.trim() || undefined,
    })
    entries.value = result.items
    total.value = result.totalCount
  } catch (err) {
    error.value = formatApiError(err)
  } finally {
    isLoading.value = false
  }
}

function applyFilters() {
  if (page.value === 1) void load()
  else page.value = 1
}

watch(page, () => void load())
onMounted(load)
</script>

<template>
  <div class="admin-page">
    <header class="page-header">
      <h1>Audit log</h1>
      <span class="muted">{{ total }} entries · append-only</span>
    </header>

    <form class="filters" @submit.prevent="applyFilters">
      <select v-model="entityType" @change="applyFilters">
        <option v-for="t in ENTITY_TYPES" :key="t" :value="t">{{ t || 'All record types' }}</option>
      </select>
      <input v-model="entityId" type="search" placeholder="Record id" class="id-input" />
      <button class="btn">Filter</button>
    </form>

    <div v-if="error" class="form-error">{{ error }}</div>
    <div v-else-if="isLoading && !entries.length" class="status">Loading…</div>

    <div v-if="entries.length" class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>When</th>
            <th>Who</th>
            <th>Action</th>
            <th>Record</th>
            <th>Changed</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="e in entries" :key="e.id">
            <tr class="entry" @click="toggle(e.id)">
              <td class="nowrap">{{ formatDateTimeEU(e.occurredAt) }}</td>
              <td>{{ e.userName }}</td>
              <td><span class="action" :data-action="e.action">{{ e.action }}</span></td>
              <td>
                <div>{{ e.entityType }}</div>
                <div class="muted mono">{{ e.entityId }}</div>
              </td>
              <td class="muted">{{ summary(e) }} <span class="chev">{{ expanded.has(e.id) ? '▾' : '▸' }}</span></td>
            </tr>
            <tr v-if="expanded.has(e.id)" class="detail">
              <td colspan="5">
                <table class="changes">
                  <thead>
                    <tr><th>Field</th><th>Before</th><th>After</th></tr>
                  </thead>
                  <tbody>
                    <tr v-for="c in parseChanges(e.changes)" :key="c.field">
                      <td>{{ c.field }}</td>
                      <template v-if="c.value !== undefined">
                        <td colspan="2">{{ show(c.value) }}</td>
                      </template>
                      <template v-else>
                        <td class="old">{{ show(c.old) }}</td>
                        <td class="new">{{ show(c.new) }}</td>
                      </template>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
    <p v-else-if="!isLoading && !error" class="status">No entries match these filters.</p>

    <nav v-if="pageCount > 1" class="pager">
      <button class="btn small" :disabled="page <= 1" @click="page--">Previous</button>
      <span class="muted">Page {{ page }} of {{ pageCount }}</span>
      <button class="btn small" :disabled="page >= pageCount" @click="page++">Next</button>
    </nav>
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
  gap: 14px;
}

.page-header { display: flex; align-items: baseline; gap: 12px; }
.page-header h1 { font-size: 1.4rem; font-weight: 600; color: #f1f5f9; }

.filters { display: flex; flex-wrap: wrap; gap: 8px; }
.id-input { flex: 1 1 260px; max-width: 420px; }

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
select:focus { outline: none; border-color: #6366f1; }

.btn {
  background: #232735;
  color: #e2e8f0;
  border: 1px solid #2d3148;
  padding: 7px 12px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.85rem;
  cursor: pointer;
}

.btn:hover:not(:disabled) { background: #2d3148; }
.btn:disabled { opacity: 0.5; cursor: not-allowed; }
.btn.small { padding: 4px 10px; font-size: 0.8rem; }

.table-wrap { overflow-x: auto; border: 1px solid #2d3148; border-radius: 10px; }

table { width: 100%; border-collapse: collapse; font-size: 0.84rem; }

th {
  text-align: left;
  font-weight: 500;
  color: #94a3b8;
  background: #161922;
  padding: 9px 12px;
  border-bottom: 1px solid #2d3148;
}

td { padding: 9px 12px; border-bottom: 1px solid #232735; vertical-align: top; }

tr.entry { cursor: pointer; }
tr.entry:hover td { background: #161922; }
tr.detail > td { background: #12141c; }

.changes { font-size: 0.8rem; border: 1px solid #232735; border-radius: 6px; }
.changes th { background: #0f1117; }
.changes td { word-break: break-word; }
.old { color: #fca5a5; }
.new { color: #86efac; }

.action { font-size: 0.72rem; padding: 2px 8px; border-radius: 999px; border: 1px solid #334155; }
.action[data-action='Created'] { background: #052e16; color: #bbf7d0; border-color: #166534; }
.action[data-action='Updated'] { background: #172554; color: #bfdbfe; border-color: #1e40af; }
.action[data-action='Deleted'] { background: #3b1219; color: #fecaca; border-color: #7f1d1d; }

.muted { color: #94a3b8; font-size: 0.78rem; }
.mono { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; }
.nowrap { white-space: nowrap; }
.chev { margin-left: 4px; }

.pager { display: flex; align-items: center; gap: 12px; justify-content: center; }

.form-error {
  background: #3b1219;
  border: 1px solid #7f1d1d;
  color: #fecaca;
  font-size: 0.82rem;
  padding: 8px 10px;
  border-radius: 6px;
}

.status { color: #94a3b8; }
</style>
