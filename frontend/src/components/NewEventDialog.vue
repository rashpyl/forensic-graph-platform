<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'

import { useEventsStore } from '@/stores/events'
import { useUiStore } from '@/stores/ui'
import { formatApiError } from '@/services/apiError'
import { localDateTimeToUtcIso } from '@/composables/useDateTime'
import { colorForSeverity, labelForSeverity } from '@/composables/useSeverity'
import type { CrimeEventWriteDto } from '@/types/api'

/**
 * Modal form for creating a new crime event. Coordinates are captured on
 * the map first and handed off via <c>ui.pendingLocation</c>; this component
 * assembles the rest of the write DTO and POSTs to /api/events.
 *
 * Backend validation errors (FluentValidation) surface inline via the
 * <see cref="formatApiError"/> helper — no toast library needed for the MVP.
 */

const uiStore = useUiStore()
const eventsStore = useEventsStore()
const { isNewEventDialogOpen, pendingLocation } = storeToRefs(uiStore)

const title = ref('')
const description = ref('')
const address = ref('')
const occurredAtLocal = ref('')
const severity = ref(3)
const submitting = ref(false)
const error = ref<string | null>(null)

const coordsLabel = computed(() => {
  if (!pendingLocation.value) return ''
  const { lat, lng } = pendingLocation.value
  return `${lat.toFixed(5)}, ${lng.toFixed(5)}`
})

function resetForm() {
  title.value = ''
  description.value = ''
  address.value = ''
  occurredAtLocal.value = toLocalInputValue(new Date())
  severity.value = 3
  error.value = null
  submitting.value = false
}

function toLocalInputValue(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

watch(isNewEventDialogOpen, (open) => {
  if (open) {
    resetForm()
  }
})

function close() {
  uiStore.closeNewEventDialog()
}

async function submit() {
  if (!pendingLocation.value) return
  error.value = null

  const occurredIso = localDateTimeToUtcIso(occurredAtLocal.value)
  if (!occurredIso) {
    error.value = 'Please provide the date and time the event occurred.'
    return
  }
  if (!title.value.trim()) {
    error.value = 'Please provide a title.'
    return
  }

  const body: CrimeEventWriteDto = {
    title: title.value.trim(),
    description: description.value.trim() || null,
    address: address.value.trim() || null,
    occurredAt: occurredIso,
    severity: severity.value,
    latitude: pendingLocation.value.lat,
    longitude: pendingLocation.value.lng,
  }

  submitting.value = true
  try {
    const created = await eventsStore.create(body)
    close()
    // Open the newly-created event in the side panel.
    await eventsStore.select(created.id)
  } catch (err) {
    error.value = formatApiError(err)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div v-if="isNewEventDialogOpen" class="dialog-backdrop" @click.self="close">
    <div class="dialog" role="dialog" aria-labelledby="new-event-title">
      <header class="dialog-header">
        <h2 id="new-event-title">New crime event</h2>
        <button class="icon-btn" @click="close" aria-label="Close">✕</button>
      </header>

      <div class="dialog-body">
        <div class="coords-summary">
          <span class="coords-label">Location:</span>
          <span class="coords-value">{{ coordsLabel }}</span>
        </div>

        <label class="field">
          <span class="field-label">Title *</span>
          <input
            v-model="title"
            type="text"
            maxlength="200"
            placeholder="e.g. Burglary at residential address"
            required
          />
        </label>

        <label class="field">
          <span class="field-label">Description</span>
          <textarea
            v-model="description"
            rows="3"
            maxlength="2000"
            placeholder="Free-text summary — witnesses, evidence, status…"
          />
        </label>

        <label class="field">
          <span class="field-label">Address</span>
          <input
            v-model="address"
            type="text"
            maxlength="500"
            placeholder="e.g. Wenceslas Square 42, Prague"
          />
        </label>

        <div class="field-row">
          <label class="field">
            <span class="field-label">Occurred at *</span>
            <input
              v-model="occurredAtLocal"
              type="datetime-local"
              required
            />
          </label>

          <label class="field">
            <span class="field-label">Severity</span>
            <select v-model.number="severity">
              <option v-for="level in [1, 2, 3, 4, 5]" :key="level" :value="level">
                {{ level }} — {{ labelForSeverity(level) }}
              </option>
            </select>
            <span
              class="severity-swatch"
              :style="{ background: colorForSeverity(severity) }"
              :title="labelForSeverity(severity)"
            />
          </label>
        </div>

        <div v-if="error" class="form-error">{{ error }}</div>
      </div>

      <footer class="dialog-footer">
        <button class="btn ghost" @click="close" :disabled="submitting">Cancel</button>
        <button class="btn primary" @click="submit" :disabled="submitting">
          {{ submitting ? 'Saving…' : 'Create event' }}
        </button>
      </footer>
    </div>
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
  width: min(520px, 92vw);
  max-height: 90vh;
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
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.coords-summary {
  background: #232735;
  border: 1px solid #2d3148;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: 0.85rem;
  display: flex;
  gap: 8px;
}

.coords-label { color: #94a3b8; }
.coords-value { color: #e2e8f0; font-family: 'SF Mono', Menlo, monospace; }

.field {
  display: flex;
  flex-direction: column;
  gap: 4px;
  position: relative;
}

.field-label {
  font-size: 0.8rem;
  color: #94a3b8;
  letter-spacing: 0.03em;
}

.field input,
.field textarea,
.field select {
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 8px 10px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.9rem;
}

.field input:focus,
.field textarea:focus,
.field select:focus {
  outline: none;
  border-color: #6366f1;
}

.field textarea { resize: vertical; }

.field-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.severity-swatch {
  position: absolute;
  right: 10px;
  top: 32px;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  border: 1px solid #0f172a;
  pointer-events: none;
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

.btn.primary {
  background: #6366f1;
  color: white;
}

.btn.primary:hover:not(:disabled) { background: #4f46e5; }

.btn.ghost {
  background: transparent;
  border-color: #2d3148;
  color: #cbd5e1;
}

.btn.ghost:hover:not(:disabled) { background: #232735; }

.btn:disabled { opacity: 0.6; cursor: not-allowed; }
</style>
