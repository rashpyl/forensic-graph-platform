<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'

import { useEventsStore } from '@/stores/events'
import { usePersonsStore } from '@/stores/persons'
import { formatApiError } from '@/services/apiError'
import { formatDateTimeEU } from '@/composables/useDateTime'
import { colorForSeverity, labelForSeverity } from '@/composables/useSeverity'
import type { EventRole, PersonDto } from '@/types/api'

/**
 * Right-side detail panel. Slides in whenever
 * <c>eventsStore.selected</c> is non-null. Owns three edit affordances:
 *   1. Add-person: autocomplete → POST /api/events/{id}/persons
 *   2. Link-event: existing-event picker → POST /api/events/{id}/links
 *   3. Delete event: DELETE /api/events/{id} with browser confirm()
 * Reads on every mutation stay eventually consistent by re-fetching the
 * selected event's detail projection through
 * <c>eventsStore.refreshSelected()</c>.
 */

const eventsStore = useEventsStore()
const personsStore = usePersonsStore()
const { selected, events } = storeToRefs(eventsStore)

const ROLES: EventRole[] = [
  'victim',
  'suspect',
  'witness',
  'perpetrator',
  'reporter',
  'officer',
]

// ── Add person ──────────────────────────────────────────────────
const personQuery = ref('')
const personRole = ref<EventRole>('victim')
const personSuggestions = ref<PersonDto[]>([])
const selectedPerson = ref<PersonDto | null>(null)
const personSearchDebounce = ref<ReturnType<typeof setTimeout> | null>(null)
const isAddingPerson = ref(false)
const addPersonError = ref<string | null>(null)

watch(personQuery, (q) => {
  selectedPerson.value = null
  if (personSearchDebounce.value) clearTimeout(personSearchDebounce.value)
  if (!q.trim()) {
    personSuggestions.value = []
    return
  }
  personSearchDebounce.value = setTimeout(async () => {
    try {
      personSuggestions.value = await personsStore.search(q.trim())
    } catch {
      personSuggestions.value = []
    }
  }, 200)
})

function pickPerson(p: PersonDto) {
  selectedPerson.value = p
  personQuery.value = `${p.firstName} ${p.lastName}`
  personSuggestions.value = []
}

async function addPerson() {
  if (!selected.value || !selectedPerson.value) {
    addPersonError.value = 'Pick a person from the suggestions first.'
    return
  }
  isAddingPerson.value = true
  addPersonError.value = null
  try {
    await eventsStore.assignPerson(
      selected.value.id,
      selectedPerson.value.id,
      personRole.value,
    )
    personQuery.value = ''
    selectedPerson.value = null
    personSuggestions.value = []
  } catch (err) {
    addPersonError.value = formatApiError(err)
  } finally {
    isAddingPerson.value = false
  }
}

async function removePerson(personId: string, role: EventRole) {
  if (!selected.value) return
  try {
    await eventsStore.unassignPerson(selected.value.id, personId, role)
  } catch (err) {
    addPersonError.value = formatApiError(err)
  }
}

// ── Link event ──────────────────────────────────────────────────
const linkTargetId = ref<string>('')
const linkNote = ref<string>('')
const isLinking = ref(false)
const linkError = ref<string | null>(null)

const linkCandidates = computed(() =>
  events.value.filter(
    (e) =>
      e.id !== selected.value?.id &&
      !selected.value?.links.some((l) => l.toEventId === e.id),
  ),
)

async function linkEvent() {
  if (!selected.value || !linkTargetId.value) {
    linkError.value = 'Pick a target event to link.'
    return
  }
  isLinking.value = true
  linkError.value = null
  try {
    await eventsStore.linkEvent(
      selected.value.id,
      linkTargetId.value,
      linkNote.value.trim() || null,
    )
    linkTargetId.value = ''
    linkNote.value = ''
  } catch (err) {
    linkError.value = formatApiError(err)
  } finally {
    isLinking.value = false
  }
}

async function unlink(toEventId: string) {
  if (!selected.value) return
  try {
    await eventsStore.unlinkEvent(selected.value.id, toEventId)
  } catch (err) {
    linkError.value = formatApiError(err)
  }
}

// ── Delete event ────────────────────────────────────────────────
const deleteError = ref<string | null>(null)

async function deleteEvent() {
  if (!selected.value) return
  const ok = window.confirm(
    `Delete "${selected.value.title}"?\n\nThis removes the event and all its person assignments and outgoing links.`,
  )
  if (!ok) return
  try {
    await eventsStore.remove(selected.value.id)
  } catch (err) {
    deleteError.value = formatApiError(err)
  }
}

// Reset local state when the selection changes.
watch(selected, () => {
  personQuery.value = ''
  personSuggestions.value = []
  selectedPerson.value = null
  addPersonError.value = null
  linkTargetId.value = ''
  linkNote.value = ''
  linkError.value = null
  deleteError.value = null
})

function close() {
  eventsStore.clearSelection()
}
</script>

<template>
  <aside v-if="selected" class="detail-panel" role="complementary">
    <header class="panel-header">
      <div class="header-left">
        <span
          class="severity-badge"
          :style="{ background: colorForSeverity(selected.severity) }"
          :title="labelForSeverity(selected.severity)"
        >{{ selected.severity }}</span>
        <h2 class="panel-title">{{ selected.title }}</h2>
      </div>
      <button class="icon-btn" @click="close" aria-label="Close panel">✕</button>
    </header>

    <div class="panel-body">
      <!-- Summary -->
      <section class="panel-section">
        <div class="meta">
          <div class="meta-row">
            <span class="meta-label">Occurred</span>
            <span class="meta-value">{{ formatDateTimeEU(selected.occurredAt) }}</span>
          </div>
          <div v-if="selected.address" class="meta-row">
            <span class="meta-label">Address</span>
            <span class="meta-value">{{ selected.address }}</span>
          </div>
          <div class="meta-row">
            <span class="meta-label">Coordinates</span>
            <span class="meta-value mono">
              {{ selected.latitude?.toFixed(5) }}, {{ selected.longitude?.toFixed(5) }}
            </span>
          </div>
          <div class="meta-row">
            <span class="meta-label">Severity</span>
            <span class="meta-value">{{ labelForSeverity(selected.severity) }}</span>
          </div>
        </div>
        <p v-if="selected.description" class="description">{{ selected.description }}</p>
      </section>

      <!-- Persons -->
      <section class="panel-section">
        <h3 class="section-title">Persons ({{ selected.persons.length }})</h3>
        <ul v-if="selected.persons.length" class="person-list">
          <li v-for="p in selected.persons" :key="`${p.personId}-${p.role}`" class="person-row">
            <span class="person-name">{{ p.firstName }} {{ p.lastName }}</span>
            <span class="role-badge" :data-role="p.role">{{ p.role }}</span>
            <button class="icon-btn small" @click="removePerson(p.personId, p.role)" aria-label="Remove">✕</button>
          </li>
        </ul>
        <p v-else class="empty">No persons assigned yet.</p>

        <div class="add-row">
          <div class="autocomplete">
            <input
              v-model="personQuery"
              type="text"
              placeholder="Search for a person…"
              autocomplete="off"
            />
            <ul v-if="personSuggestions.length && !selectedPerson" class="autocomplete-list">
              <li
                v-for="s in personSuggestions"
                :key="s.id"
                class="autocomplete-item"
                @click="pickPerson(s)"
              >
                {{ s.firstName }} {{ s.lastName }}
                <span v-if="s.citizenships.length" class="autocomplete-hint">
                  ({{ s.citizenships.join(', ') }})
                </span>
              </li>
            </ul>
          </div>
          <select v-model="personRole" class="role-select">
            <option v-for="r in ROLES" :key="r" :value="r">{{ r }}</option>
          </select>
          <button
            class="btn primary small"
            @click="addPerson"
            :disabled="isAddingPerson || !selectedPerson"
          >
            {{ isAddingPerson ? '…' : 'Add' }}
          </button>
        </div>
        <div v-if="addPersonError" class="form-error small">{{ addPersonError }}</div>
      </section>

      <!-- Links -->
      <section class="panel-section">
        <h3 class="section-title">Linked events ({{ selected.links.length }})</h3>
        <ul v-if="selected.links.length" class="link-list">
          <li v-for="l in selected.links" :key="l.toEventId" class="link-row">
            <div class="link-content">
              <span class="link-title">{{ l.toEventTitle }}</span>
              <span v-if="l.note" class="link-note">{{ l.note }}</span>
            </div>
            <button class="icon-btn small" @click="unlink(l.toEventId)" aria-label="Unlink">✕</button>
          </li>
        </ul>
        <p v-else class="empty">No linked events.</p>

        <div class="add-row column">
          <select v-model="linkTargetId" class="full-width">
            <option value="" disabled>Pick an event to link…</option>
            <option v-for="e in linkCandidates" :key="e.id" :value="e.id">{{ e.title }}</option>
          </select>
          <input
            v-model="linkNote"
            type="text"
            placeholder="Optional note (e.g. same suspect)"
            maxlength="500"
          />
          <button
            class="btn primary small self-end"
            @click="linkEvent"
            :disabled="isLinking || !linkTargetId"
          >
            {{ isLinking ? '…' : 'Link event' }}
          </button>
        </div>
        <div v-if="linkError" class="form-error small">{{ linkError }}</div>
      </section>

      <!-- Danger zone -->
      <section class="panel-section danger-zone">
        <h3 class="section-title danger">Danger zone</h3>
        <button class="btn danger" @click="deleteEvent">Delete event</button>
        <div v-if="deleteError" class="form-error small">{{ deleteError }}</div>
      </section>
    </div>
  </aside>
</template>

<style scoped>
.detail-panel {
  width: 380px;
  height: 100%;
  background: #1a1d27;
  border-left: 1px solid #2d3148;
  color: #e2e8f0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  z-index: 800;
  box-shadow: -4px 0 16px rgba(0, 0, 0, 0.35);
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 14px 16px;
  border-bottom: 1px solid #2d3148;
  gap: 8px;
}

.header-left {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  min-width: 0;
}

.severity-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  color: #0f172a;
  font-weight: 700;
  font-size: 0.85rem;
  border: 1px solid #0f172a;
  flex-shrink: 0;
}

.panel-title {
  font-size: 0.95rem;
  font-weight: 600;
  color: #f1f5f9;
  line-height: 1.35;
  word-break: break-word;
}

.icon-btn {
  background: transparent;
  border: none;
  color: #94a3b8;
  cursor: pointer;
  font-size: 0.9rem;
  padding: 4px 8px;
  border-radius: 4px;
  flex-shrink: 0;
}

.icon-btn.small { font-size: 0.75rem; padding: 2px 6px; }
.icon-btn:hover { background: #2d3148; color: #e2e8f0; }

.panel-body {
  padding: 8px 0;
  overflow-y: auto;
  flex: 1;
}

.panel-section {
  padding: 14px 16px;
  border-bottom: 1px solid #2d3148;
}

.panel-section:last-child { border-bottom: none; }

.section-title {
  font-size: 0.8rem;
  color: #a5b4fc;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 10px;
  font-weight: 600;
}

.section-title.danger { color: #f87171; }

.meta { display: flex; flex-direction: column; gap: 6px; }
.meta-row { display: flex; gap: 10px; font-size: 0.85rem; }
.meta-label { color: #94a3b8; min-width: 90px; }
.meta-value { color: #e2e8f0; }
.meta-value.mono { font-family: 'SF Mono', Menlo, monospace; font-size: 0.8rem; }

.description {
  margin-top: 10px;
  font-size: 0.85rem;
  color: #cbd5e1;
  line-height: 1.4;
  white-space: pre-wrap;
}

.empty { color: #64748b; font-size: 0.85rem; font-style: italic; }

.person-list, .link-list {
  list-style: none;
  margin: 0 0 10px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.person-row, .link-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 8px;
  background: #232735;
  border: 1px solid #2d3148;
  border-radius: 6px;
  font-size: 0.85rem;
}

.person-name { flex: 1; }

.role-badge {
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  background: #2d3148;
  color: #cbd5e1;
}

.role-badge[data-role='victim']      { background: #7f1d1d; color: #fecaca; }
.role-badge[data-role='suspect']     { background: #78350f; color: #fed7aa; }
.role-badge[data-role='perpetrator'] { background: #450a0a; color: #fca5a5; }
.role-badge[data-role='witness']     { background: #1e40af; color: #bfdbfe; }
.role-badge[data-role='reporter']    { background: #14532d; color: #bbf7d0; }
.role-badge[data-role='officer']     { background: #312e81; color: #c7d2fe; }

.link-content { flex: 1; display: flex; flex-direction: column; gap: 2px; }
.link-title { color: #e2e8f0; }
.link-note { color: #94a3b8; font-size: 0.75rem; }

.add-row {
  display: grid;
  grid-template-columns: 1fr auto auto;
  gap: 6px;
  align-items: start;
}

.add-row.column {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.autocomplete { position: relative; }

.autocomplete input,
.role-select,
.add-row input,
.add-row select {
  width: 100%;
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 6px 8px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.85rem;
}

.autocomplete input:focus,
.role-select:focus,
.add-row input:focus,
.add-row select:focus {
  outline: none;
  border-color: #6366f1;
}

.autocomplete-list {
  position: absolute;
  left: 0;
  right: 0;
  top: calc(100% + 2px);
  background: #232735;
  border: 1px solid #2d3148;
  border-radius: 6px;
  max-height: 180px;
  overflow-y: auto;
  z-index: 900;
  list-style: none;
  margin: 0;
  padding: 4px 0;
}

.autocomplete-item {
  padding: 6px 10px;
  font-size: 0.85rem;
  cursor: pointer;
}

.autocomplete-item:hover { background: #2d3148; }

.autocomplete-hint { color: #94a3b8; font-size: 0.75rem; margin-left: 4px; }

.role-select { min-width: 100px; }
.full-width { width: 100%; }
.self-end { align-self: flex-end; }

.btn {
  padding: 6px 12px;
  border-radius: 6px;
  border: 1px solid transparent;
  font-size: 0.8rem;
  cursor: pointer;
}

.btn.small { padding: 4px 10px; }
.btn.primary { background: #6366f1; color: white; }
.btn.primary:hover:not(:disabled) { background: #4f46e5; }
.btn.danger { background: #dc2626; color: white; border-color: #b91c1c; }
.btn.danger:hover { background: #b91c1c; }
.btn:disabled { opacity: 0.55; cursor: not-allowed; }

.form-error {
  background: rgba(153, 27, 27, 0.25);
  border: 1px solid #ef4444;
  color: #fecaca;
  padding: 6px 10px;
  border-radius: 6px;
  font-size: 0.8rem;
  margin-top: 8px;
  white-space: pre-line;
}

.form-error.small { font-size: 0.75rem; }
.danger-zone { padding-top: 20px; }
</style>
