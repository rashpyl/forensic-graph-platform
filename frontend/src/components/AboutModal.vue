<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useUiStore } from '@/stores/ui'

/**
 * "About" modal shown from the navbar. Purely informational — no server
 * interaction. The copy summarises the project scope so a first-time viewer
 * understands what this MVP is, without hunting through the README.
 */
const uiStore = useUiStore()
const { isAboutModalOpen } = storeToRefs(uiStore)

function close() {
  uiStore.closeAboutModal()
}
</script>

<template>
  <div v-if="isAboutModalOpen" class="dialog-backdrop" @click.self="close">
    <div class="dialog" role="dialog" aria-labelledby="about-title">
      <header class="dialog-header">
        <h2 id="about-title">About Forensic Graph Platform</h2>
        <button class="icon-btn" @click="close" aria-label="Close">✕</button>
      </header>

      <div class="dialog-body">
        <p>
          This is a diploma-project demo of an <strong>interactive investigation map</strong>.
          Police can pin crime events on a world map, attach persons in specific
          roles (victim, suspect, witness, perpetrator, reporter, officer),
          and manually connect related events.
        </p>

        <h3>What you can do</h3>
        <ul>
          <li>Browse events across cities.</li>
          <li>Hover a marker for a compact summary; click for full detail.</li>
          <li>Create a new event with the <em>+ New Event</em> button.</li>
          <li>Assign persons to an event with role-scoped autocomplete.</li>
          <li>Link related events manually.</li>
          <li>Delete events or unassign / unlink at any time.</li>
        </ul>

        <h3>Stack</h3>
        <p>
          Vue 3 + TypeScript + Pinia + Leaflet on the frontend, ASP.NET Core 9
          + EF Core + PostgreSQL on the backend, clean-architecture layered
          (Domain / Application / Infrastructure / Api).
        </p>

        <p class="footer-note">
          Version: MVP.
        </p>
      </div>

      <footer class="dialog-footer">
        <button class="btn primary" @click="close">Got it</button>
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
  font-size: 0.9rem;
  line-height: 1.5;
  color: #cbd5e1;
}

.dialog-body h3 {
  color: #a5b4fc;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin: 14px 0 6px;
}

.dialog-body ul {
  margin: 4px 0 4px 20px;
  padding: 0;
}

.dialog-body li { margin-bottom: 4px; }

.footer-note {
  margin-top: 14px;
  color: #94a3b8;
  font-size: 0.8rem;
  font-style: italic;
}

.dialog-footer {
  padding: 12px 20px;
  border-top: 1px solid #2d3148;
  display: flex;
  justify-content: flex-end;
}

.btn {
  padding: 8px 14px;
  border-radius: 6px;
  border: 1px solid transparent;
  font-size: 0.85rem;
  cursor: pointer;
}

.btn.primary { background: #6366f1; color: white; }
.btn.primary:hover { background: #4f46e5; }
</style>
