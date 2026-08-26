<script setup lang="ts">
import { ref, watch } from 'vue'
import { storeToRefs } from 'pinia'

import { usePersonsStore } from '@/stores/persons'
import { useUiStore } from '@/stores/ui'
import { formatApiError } from '@/services/apiError'
import type { PersonWriteDto } from '@/types/api'

/**
 * Modal form for creating a new person. Multi-value fields (citizenships,
 * passport numbers) use an inline chip input: type a value, press Enter or
 * comma to commit it, click a chip's × to remove.
 *
 * Backend validation errors surface inline via <see cref="formatApiError"/>.
 */

const uiStore = useUiStore()
const personsStore = usePersonsStore()
const { isNewPersonDialogOpen } = storeToRefs(uiStore)

const firstName = ref('')
const lastName = ref('')
const citizenships = ref<string[]>([])
const passportNumbers = ref<string[]>([])
const phone = ref('')
const physicalDescription = ref('')
const submitting = ref(false)
const error = ref<string | null>(null)

const citizenshipDraft = ref('')
const passportDraft = ref('')

function reset() {
  firstName.value = ''
  lastName.value = ''
  citizenships.value = []
  passportNumbers.value = []
  phone.value = ''
  physicalDescription.value = ''
  citizenshipDraft.value = ''
  passportDraft.value = ''
  error.value = null
  submitting.value = false
}

watch(isNewPersonDialogOpen, (open) => {
  if (open) reset()
})

function close() {
  uiStore.closeNewPersonDialog()
}

function pushChip(target: string[], draft: string) {
  const trimmed = draft.trim()
  if (!trimmed) return
  if (!target.includes(trimmed)) {
    target.push(trimmed)
  }
}

function handleCitizenshipKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault()
    pushChip(citizenships.value, citizenshipDraft.value)
    citizenshipDraft.value = ''
  } else if (e.key === 'Backspace' && !citizenshipDraft.value && citizenships.value.length) {
    citizenships.value.pop()
  }
}

function handlePassportKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault()
    pushChip(passportNumbers.value, passportDraft.value)
    passportDraft.value = ''
  } else if (e.key === 'Backspace' && !passportDraft.value && passportNumbers.value.length) {
    passportNumbers.value.pop()
  }
}

function removeCitizenship(index: number) {
  citizenships.value.splice(index, 1)
}

function removePassport(index: number) {
  passportNumbers.value.splice(index, 1)
}

async function submit() {
  error.value = null
  if (!firstName.value.trim() || !lastName.value.trim()) {
    error.value = 'First name and last name are required.'
    return
  }

  // Commit any half-typed chip drafts before submitting.
  pushChip(citizenships.value, citizenshipDraft.value)
  pushChip(passportNumbers.value, passportDraft.value)
  citizenshipDraft.value = ''
  passportDraft.value = ''

  const body: PersonWriteDto = {
    firstName: firstName.value.trim(),
    lastName: lastName.value.trim(),
    citizenships: [...citizenships.value],
    passportNumbers: [...passportNumbers.value],
    phone: phone.value.trim() || null,
    physicalDescription: physicalDescription.value.trim() || null,
  }

  submitting.value = true
  try {
    await personsStore.create(body)
    close()
  } catch (err) {
    error.value = formatApiError(err)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div v-if="isNewPersonDialogOpen" class="dialog-backdrop" @click.self="close">
    <div class="dialog" role="dialog" aria-labelledby="new-person-title">
      <header class="dialog-header">
        <h2 id="new-person-title">New person</h2>
        <button class="icon-btn" @click="close" aria-label="Close">✕</button>
      </header>

      <div class="dialog-body">
        <div class="field-row">
          <label class="field">
            <span class="field-label">First name *</span>
            <input v-model="firstName" type="text" maxlength="100" required />
          </label>
          <label class="field">
            <span class="field-label">Last name *</span>
            <input v-model="lastName" type="text" maxlength="100" required />
          </label>
        </div>

        <div class="field">
          <span class="field-label">Citizenships</span>
          <div class="chip-input" @click="() => (($refs.citInput as HTMLInputElement).focus())">
            <span v-for="(c, i) in citizenships" :key="`${c}-${i}`" class="chip">
              {{ c }}
              <button type="button" class="chip-x" @click.stop="removeCitizenship(i)">×</button>
            </span>
            <input
              ref="citInput"
              v-model="citizenshipDraft"
              type="text"
              placeholder="Type and press Enter or comma"
              class="chip-input-field"
              maxlength="60"
              @keydown="handleCitizenshipKeydown"
            />
          </div>
        </div>

        <div class="field">
          <span class="field-label">Passport numbers</span>
          <div class="chip-input" @click="() => (($refs.paspInput as HTMLInputElement).focus())">
            <span v-for="(p, i) in passportNumbers" :key="`${p}-${i}`" class="chip">
              {{ p }}
              <button type="button" class="chip-x" @click.stop="removePassport(i)">×</button>
            </span>
            <input
              ref="paspInput"
              v-model="passportDraft"
              type="text"
              placeholder="Type and press Enter or comma"
              class="chip-input-field"
              maxlength="40"
              @keydown="handlePassportKeydown"
            />
          </div>
        </div>

        <label class="field">
          <span class="field-label">Phone</span>
          <input v-model="phone" type="tel" maxlength="40" placeholder="+420 …" />
        </label>

        <label class="field">
          <span class="field-label">Physical description</span>
          <textarea
            v-model="physicalDescription"
            rows="3"
            maxlength="2000"
            placeholder="Height, build, distinctive features…"
          />
        </label>

        <div v-if="error" class="form-error">{{ error }}</div>
      </div>

      <footer class="dialog-footer">
        <button class="btn ghost" @click="close" :disabled="submitting">Cancel</button>
        <button class="btn primary" @click="submit" :disabled="submitting">
          {{ submitting ? 'Saving…' : 'Create person' }}
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
  width: min(560px, 92vw);
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

.field input,
.field textarea {
  background: #0f1117;
  border: 1px solid #2d3148;
  color: #e2e8f0;
  padding: 8px 10px;
  border-radius: 6px;
  font: inherit;
  font-size: 0.9rem;
}

.field input:focus,
.field textarea:focus {
  outline: none;
  border-color: #6366f1;
}

.field textarea { resize: vertical; }

.field-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}

.chip-input {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  background: #0f1117;
  border: 1px solid #2d3148;
  border-radius: 6px;
  padding: 6px 8px;
  min-height: 38px;
  cursor: text;
}

.chip-input:focus-within {
  border-color: #6366f1;
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: #312e81;
  color: #c7d2fe;
  padding: 2px 6px 2px 8px;
  border-radius: 12px;
  font-size: 0.8rem;
}

.chip-x {
  background: transparent;
  border: none;
  color: #c7d2fe;
  cursor: pointer;
  font-size: 0.9rem;
  line-height: 1;
  padding: 0 2px;
}

.chip-x:hover { color: white; }

.chip-input-field {
  flex: 1;
  min-width: 120px;
  background: transparent !important;
  border: none !important;
  padding: 2px 0 !important;
  color: #e2e8f0;
  font: inherit;
  font-size: 0.85rem;
}

.chip-input-field:focus { outline: none; }

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
