<template>
  <div class="editor-layout">

    <!-- ── Horizontal toolbar ── -->
    <div class="toolbar">
      <div class="tool-group">
        <button class="tool-btn" :class="{ active: mode === 'select' }" @click="setMode('select')">
          <span class="t-icon">↖</span> Select
        </button>
        <button class="tool-btn" :class="{ active: mode === 'add' }" @click="openCreateModal">
          <span class="t-icon">＋</span> Add Event
        </button>
        <button class="tool-btn" :class="{ active: mode === 'connect' }" @click="setMode('connect')">
          <span class="t-icon">⟶</span> Connect
        </button>
        <button class="tool-btn danger" :class="{ active: mode === 'delete' }" @click="setMode('delete')">
          <span class="t-icon">✕</span> Delete
        </button>
      </div>

      <div class="tool-status">
        <span class="mode-label">{{ mode }}</span>
        <span v-if="mode === 'connect' && connectSource" class="connect-hint">→ click target node</span>
      </div>
    </div>

    <!-- ── Content row: canvas + panel ── -->
    <div class="content-row">
      <div ref="cyContainer" class="cy-canvas" />

      <!-- Side panel -->
      <aside class="side-panel">
        <!-- Node selected -->
        <template v-if="panelNode">
          <div class="panel-header">
            <span class="badge red">crime event</span>
            <button class="icon-btn" @click="panelNode = null">✕</button>
          </div>
          <h3 class="panel-title">{{ panelNode.title }}</h3>
          <div class="field">
            <span class="field-label">📅 Date &amp; Time</span>
            <span class="field-value">{{ formatDateTime(panelNode.occurredAt) || '—' }}</span>
          </div>
          <div class="field grow">
            <span class="field-label">📝 Description</span>
            <p class="field-text">{{ panelNode.description || 'No description.' }}</p>
          </div>
          <button class="delete-btn" @click="deletePanel">🗑 Delete node</button>
        </template>

        <!-- Edge selected -->
        <template v-else-if="panelEdge">
          <div class="panel-header">
            <span class="badge blue">connection</span>
            <button class="icon-btn" @click="panelEdge = null">✕</button>
          </div>
          <h3 class="panel-title edge-title">
            {{ getNodeTitle(panelEdge.sourceId) }}
            <span class="arrow">→</span>
            {{ getNodeTitle(panelEdge.targetId) }}
          </h3>
          <button class="delete-btn" @click="deletePanelEdge">🗑 Delete connection</button>
        </template>

        <!-- Empty -->
        <template v-else>
          <p class="empty-hint">Click a node or connection to see its details.</p>
        </template>
      </aside>
    </div>

  </div>

  <!-- Create event modal -->
  <Teleport to="body">
    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal">
        <h2>New Crime Event</h2>
        <label>Title<input v-model="form.title" placeholder="e.g. Robbery at station" autofocus /></label>
        <label>Date &amp; Time<input v-model="form.occurredAt" type="datetime-local" /></label>
        <label>Description<textarea v-model="form.description" placeholder="Details…" rows="4" /></label>
        <div class="modal-actions">
          <button class="btn-secondary" @click="closeModal">Cancel</button>
          <button class="btn-primary" :disabled="!form.title.trim()" @click="submitCreate">Create</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import cytoscape from 'cytoscape'
import { useGraphStore, type GraphNode, type GraphEdge } from '../stores/graphStore'

const store = useGraphStore()
const cyContainer = ref<HTMLElement | null>(null)
let cy: cytoscape.Core | null = null

type Mode = 'select' | 'add' | 'connect' | 'delete'
const mode = ref<Mode>('select')
const connectSource = ref<string | null>(null)
const showModal = ref(false)
const form = ref({ title: '', description: '', occurredAt: '' })

const panelNode = ref<GraphNode | null>(null)
const panelEdge = ref<GraphEdge | null>(null)

function showNode(id: string) {
  panelNode.value = store.nodes.find(n => n.id === id) ?? null
  panelEdge.value = null
}
function showEdge(id: string) {
  panelEdge.value = store.edges.find(e => e.id === id) ?? null
  panelNode.value = null
}
function clearPanel() { panelNode.value = null; panelEdge.value = null }

// ── Cytoscape ─────────────────────────────────────────────────────────────────

onMounted(() => {
  cy = cytoscape({
    container: cyContainer.value!,
    style: [
      {
        selector: 'node',
        style: {
          'background-color': '#e53e3e', 'border-color': '#fc8181', 'border-width': 2,
          label: 'data(label)', color: '#fff', 'text-valign': 'center', 'text-halign': 'center',
          'font-size': 11, 'text-wrap': 'wrap', 'text-max-width': '80px', width: 60, height: 60,
        } as cytoscape.Css.Node,
      },
      { selector: 'node.selected', style: { 'border-color': '#a5b4fc', 'border-width': 3 } as cytoscape.Css.Node },
      { selector: 'node.connect-source', style: { 'border-color': '#68d391', 'border-width': 3, 'background-color': '#276749' } as cytoscape.Css.Node },
      {
        selector: 'edge',
        style: {
          'line-color': '#4a5568', 'target-arrow-color': '#4a5568',
          'target-arrow-shape': 'triangle', 'curve-style': 'bezier', width: 2,
        } as cytoscape.Css.Edge,
      },
      { selector: 'edge.selected', style: { 'line-color': '#a5b4fc', 'target-arrow-color': '#a5b4fc', width: 3 } as cytoscape.Css.Edge },
    ],
    layout: { name: 'preset' },
    userZoomingEnabled: true,
    userPanningEnabled: true,
  })

  cy.on('tap', 'node', (e) => {
    const id: string = e.target.id()
    if (mode.value === 'delete') { deleteNode(id); return }
    if (mode.value === 'connect') {
      if (!connectSource.value) {
        connectSource.value = id
        cy!.$('.connect-source').removeClass('connect-source')
        cy!.$(`#${id}`).addClass('connect-source')
      } else if (connectSource.value !== id) {
        createEdge(connectSource.value, id)
        clearConnectSource()
      }
      return
    }
    cy!.$('.selected').removeClass('selected')
    cy!.$(`#${id}`).addClass('selected')
    showNode(id)
  })

  cy.on('tap', 'edge', (e) => {
    const id: string = e.target.id()
    if (mode.value === 'delete') { deleteEdge(id); return }
    cy!.$('.selected').removeClass('selected')
    cy!.$(`#${id}`).addClass('selected')
    showEdge(id)
  })

  cy.on('tap', (e) => {
    if (e.target !== cy) return
    cy!.$('.selected').removeClass('selected')
    clearPanel()
    clearConnectSource()
  })

  window.addEventListener('keydown', onKeyDown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', onKeyDown)
  cy?.destroy()
})

// ── Helpers ───────────────────────────────────────────────────────────────────

function setMode(m: Mode) {
  mode.value = m
  clearConnectSource()
  clearPanel()
  cy?.$('.selected').removeClass('selected')
}

function clearConnectSource() {
  if (connectSource.value) cy?.$(`#${connectSource.value}`).removeClass('connect-source')
  connectSource.value = null
}

function onKeyDown(e: KeyboardEvent) {
  if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return
  if (e.key === 'Delete' || e.key === 'Backspace') {
    if (panelNode.value) deleteNode(panelNode.value.id)
    else if (panelEdge.value) deleteEdge(panelEdge.value.id)
  }
  if (e.key === 'a' || e.key === 'A') openCreateModal()
  if (e.key === 'c' || e.key === 'C') setMode('connect')
  if (e.key === 's' || e.key === 'S') setMode('select')
  if (e.key === 'Escape') { closeModal(); clearConnectSource(); setMode('select') }
}

// ── CRUD ──────────────────────────────────────────────────────────────────────

function openCreateModal() {
  form.value = { title: '', description: '', occurredAt: '' }
  showModal.value = true
  mode.value = 'add'
}

function closeModal() { showModal.value = false; mode.value = 'select' }

function submitCreate() {
  if (!form.value.title.trim()) return
  const node = store.addNode(form.value.title.trim(), form.value.description.trim(), form.value.occurredAt)
  const extent = cy?.extent() ?? { x1: 0, y1: 0, x2: 600, y2: 400 }
  cy?.add({
    data: { id: node.id, label: node.title },
    position: {
      x: (extent.x1 + extent.x2) / 2 + (Math.random() - 0.5) * 200,
      y: (extent.y1 + extent.y2) / 2 + (Math.random() - 0.5) * 200,
    },
  })
  closeModal()
  cy!.$('.selected').removeClass('selected')
  cy!.$(`#${node.id}`).addClass('selected')
  showNode(node.id)
}

function createEdge(sourceId: string, targetId: string) {
  const edge = store.addEdge(sourceId, targetId)
  if (edge) cy?.add({ data: { id: edge.id, source: edge.sourceId, target: edge.targetId } })
}

function deleteNode(id: string) {
  store.removeNode(id); cy?.remove(`#${id}`)
  if (panelNode.value?.id === id) clearPanel()
}
function deleteEdge(id: string) {
  store.removeEdge(id); cy?.remove(`#${id}`)
  if (panelEdge.value?.id === id) clearPanel()
}
function deletePanel() { if (panelNode.value) deleteNode(panelNode.value.id) }
function deletePanelEdge() { if (panelEdge.value) deleteEdge(panelEdge.value.id) }
function getNodeTitle(id: string) { return store.nodes.find(n => n.id === id)?.title ?? id }

function formatDateTime(iso: string): string {
  if (!iso) return ''
  const d = new Date(iso)
  const dd = String(d.getDate()).padStart(2, '0')
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const hh = String(d.getHours()).padStart(2, '0')
  const min = String(d.getMinutes()).padStart(2, '0')
  return `${dd}/${mm}/${d.getFullYear()}, ${hh}:${min}`
}
</script>

<style scoped>
/* ── Layout ── */
.editor-layout {
  display: flex;
  flex-direction: column;
  flex: 1;
  height: 100%;
  overflow: hidden;
}

.content-row {
  display: flex;
  flex: 1;
  overflow: hidden;
  min-height: 0;
}

/* ── Horizontal toolbar ── */
.toolbar {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 0 16px;
  height: 48px;
  background: #13151f;
  border-bottom: 1px solid #2d3148;
  flex-shrink: 0;
}

.tool-group {
  display: flex;
  gap: 6px;
}

.tool-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border: 1px solid #2d3148;
  border-radius: 6px;
  background: transparent;
  color: #94a3b8;
  font-size: 0.82rem;
  font-weight: 500;
  cursor: pointer;
  white-space: nowrap;
  transition: all 0.15s;
}
.tool-btn:hover { background: #252840; color: #e2e8f0; border-color: #4a5568; }
.tool-btn.active { background: #2d3580; border-color: #a5b4fc; color: #a5b4fc; }
.tool-btn.danger.active { background: #7f1d1d; border-color: #fc8181; color: #fc8181; }
.t-icon { font-size: 1rem; line-height: 1; }

.tool-status {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-left: auto;
  font-size: 0.75rem;
}
.mode-label {
  color: #4a5568;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  font-weight: 600;
}
.connect-hint { color: #68d391; }

/* ── Canvas ── */
.cy-canvas {
  flex: 1;
  min-width: 0;
  background: #0f1117;
  background-image: radial-gradient(#1e2133 1px, transparent 1px);
  background-size: 24px 24px;
}

/* ── Side panel ── */
.side-panel {
  width: 220px;
  flex-shrink: 0;
  background: #1a1d27;
  border-left: 1px solid #2d3148;
  padding: 16px 14px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow-y: auto;
}

.panel-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.badge {
  font-size: 0.6rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  padding: 2px 7px;
  border-radius: 99px;
}
.badge.red  { color: #fc8181; background: rgba(252,129,129,0.1); }
.badge.blue { color: #a5b4fc; background: rgba(165,180,252,0.1); }

.icon-btn {
  background: transparent;
  border: none;
  color: #64748b;
  cursor: pointer;
  font-size: 0.8rem;
  padding: 2px 5px;
  border-radius: 4px;
}
.icon-btn:hover { color: #94a3b8; background: #252840; }

.panel-title {
  font-size: 0.95rem;
  font-weight: 600;
  color: #e2e8f0;
  word-break: break-word;
  line-height: 1.4;
}
.edge-title { display: flex; align-items: center; gap: 5px; flex-wrap: wrap; }
.arrow { color: #a5b4fc; }

.field { display: flex; flex-direction: column; gap: 3px; }
.field.grow { flex: 1; }
.field-label {
  font-size: 0.65rem;
  color: #64748b;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}
.field-value { font-size: 0.82rem; color: #68d391; font-weight: 500; }
.field-text { font-size: 0.8rem; color: #94a3b8; line-height: 1.6; word-break: break-word; white-space: pre-wrap; }

.empty-hint {
  font-size: 0.75rem;
  color: #3d4568;
  text-align: center;
  margin-top: 32px;
  line-height: 1.6;
}

.delete-btn {
  padding: 7px 10px;
  background: #7f1d1d;
  border: 1px solid #fc8181;
  border-radius: 6px;
  color: #fc8181;
  font-size: 0.78rem;
  cursor: pointer;
  transition: background 0.15s;
  margin-top: auto;
}
.delete-btn:hover { background: #991b1b; }

/* ── Modal ── */
.modal-overlay {
  position: fixed; inset: 0; background: rgba(0,0,0,0.6);
  display: flex; align-items: center; justify-content: center; z-index: 100;
}
.modal {
  background: #1e2233; border: 1px solid #2d3148; border-radius: 12px;
  padding: 24px; width: 380px; display: flex; flex-direction: column;
  gap: 14px; box-shadow: 0 20px 60px rgba(0,0,0,0.5);
}
.modal h2 { font-size: 1rem; font-weight: 600; color: #e2e8f0; }
.modal label { display: flex; flex-direction: column; gap: 5px; font-size: 0.78rem; color: #94a3b8; font-weight: 500; }
.modal input, .modal textarea {
  background: #0f1117; border: 1px solid #2d3148; border-radius: 6px;
  color: #e2e8f0; padding: 7px 11px; font-size: 0.88rem; font-family: inherit;
  resize: vertical; outline: none; transition: border-color 0.15s; color-scheme: dark;
}
.modal input:focus, .modal textarea:focus { border-color: #a5b4fc; }
.modal-actions { display: flex; justify-content: flex-end; gap: 8px; }
.btn-primary {
  padding: 7px 18px; background: #3730a3; border: 1px solid #a5b4fc;
  border-radius: 6px; color: #a5b4fc; font-size: 0.82rem; cursor: pointer; transition: background 0.15s;
}
.btn-primary:hover:not(:disabled) { background: #4338ca; }
.btn-primary:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-secondary {
  padding: 7px 14px; background: transparent; border: 1px solid #2d3148;
  border-radius: 6px; color: #64748b; font-size: 0.82rem; cursor: pointer;
}
.btn-secondary:hover { border-color: #4a5568; color: #94a3b8; }
</style>
