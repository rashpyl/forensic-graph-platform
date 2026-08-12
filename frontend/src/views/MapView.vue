<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import 'leaflet.markercluster'
import 'leaflet.markercluster/dist/MarkerCluster.css'
import 'leaflet.markercluster/dist/MarkerCluster.Default.css'
import { storeToRefs } from 'pinia'

import { useEventsStore } from '@/stores/events'
import { colorForSeverity, labelForSeverity } from '@/composables/useSeverity'
import type { CrimeEventDto } from '@/types/api'

/**
 * Full-viewport Leaflet map. Displays every geolocated crime event as a
 * severity-coloured circle marker inside a marker-cluster group so the
 * viewport stays legible even in dense areas. Hover shows a compact tooltip
 * (title + address); click will eventually open a side panel — for the MVP
 * we log the id and rely on Leaflet's default popup.
 */

const eventsStore = useEventsStore()
const { geolocatedEvents, isLoading, error } = storeToRefs(eventsStore)

const mapContainer = ref<HTMLDivElement | null>(null)
let map: L.Map | null = null
let clusterGroup: L.MarkerClusterGroup | null = null

const DEFAULT_CENTER: L.LatLngExpression = [50.0755, 14.4378] // Prague
const DEFAULT_ZOOM = 5

function buildMarker(event: CrimeEventDto & { latitude: number; longitude: number }): L.CircleMarker {
  const marker = L.circleMarker([event.latitude, event.longitude], {
    radius: 9,
    color: '#0f172a',
    weight: 1.5,
    fillColor: colorForSeverity(event.severity),
    fillOpacity: 0.9,
  })

  const tooltipHtml = `
    <div class="marker-tooltip">
      <div class="tt-title">${escapeHtml(event.title)}</div>
      ${event.address ? `<div class="tt-address">${escapeHtml(event.address)}</div>` : ''}
      <div class="tt-severity">${escapeHtml(labelForSeverity(event.severity))}</div>
    </div>
  `
  marker.bindTooltip(tooltipHtml, {
    direction: 'top',
    offset: [0, -8],
    opacity: 0.95,
    className: 'crime-tooltip',
  })

  marker.on('click', () => {
    void eventsStore.select(event.id)
  })

  return marker
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function refreshMarkers() {
  if (!clusterGroup) return
  clusterGroup.clearLayers()
  for (const event of geolocatedEvents.value) {
    clusterGroup.addLayer(buildMarker(event))
  }
}

onMounted(async () => {
  if (!mapContainer.value) return

  map = L.map(mapContainer.value, {
    center: DEFAULT_CENTER,
    zoom: DEFAULT_ZOOM,
    worldCopyJump: true,
  })

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
  }).addTo(map)

  clusterGroup = L.markerClusterGroup({
    showCoverageOnHover: false,
    maxClusterRadius: 60,
  })
  map.addLayer(clusterGroup)

  try {
    await eventsStore.fetchAll()
  } catch {
    // error state is surfaced via the store; the map stays usable.
  }
  refreshMarkers()
})

watch(geolocatedEvents, () => {
  refreshMarkers()
})

onBeforeUnmount(() => {
  map?.remove()
  map = null
  clusterGroup = null
})
</script>

<template>
  <div class="map-view">
    <div ref="mapContainer" class="map-canvas" />

    <div v-if="isLoading" class="map-status loading">Loading events…</div>
    <div v-else-if="error" class="map-status error">
      Could not load events: {{ error }}
    </div>

    <div class="severity-legend">
      <div class="legend-title">Severity</div>
      <div v-for="level in [1, 2, 3, 4, 5]" :key="level" class="legend-row">
        <span class="dot" :style="{ background: colorForSeverity(level) }" />
        <span class="lbl">{{ level }} — {{ labelForSeverity(level) }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.map-view {
  position: relative;
  flex: 1;
  height: 100%;
  width: 100%;
}

.map-canvas {
  position: absolute;
  inset: 0;
  z-index: 0;
}

.map-status {
  position: absolute;
  top: 12px;
  left: 50%;
  transform: translateX(-50%);
  padding: 8px 14px;
  border-radius: 8px;
  font-size: 0.85rem;
  z-index: 500;
  background: rgba(15, 17, 23, 0.85);
  color: #e2e8f0;
  border: 1px solid #2d3148;
}

.map-status.error {
  background: rgba(153, 27, 27, 0.85);
  border-color: #ef4444;
}

.severity-legend {
  position: absolute;
  bottom: 20px;
  right: 20px;
  padding: 10px 14px;
  background: rgba(15, 17, 23, 0.9);
  border: 1px solid #2d3148;
  border-radius: 8px;
  font-size: 0.8rem;
  color: #e2e8f0;
  z-index: 500;
  min-width: 150px;
}

.legend-title {
  font-weight: 600;
  margin-bottom: 6px;
  color: #a5b4fc;
  letter-spacing: 0.03em;
}

.legend-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 2px 0;
}

.legend-row .dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  border: 1px solid #0f172a;
  flex-shrink: 0;
}
</style>

<style>
/* Global overrides for the tooltip HTML — must be un-scoped because
   Leaflet renders the tooltip outside the component's DOM tree. */
.crime-tooltip {
  background: #1a1d27 !important;
  color: #e2e8f0 !important;
  border: 1px solid #2d3148 !important;
  padding: 8px 10px !important;
  border-radius: 6px !important;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.4) !important;
  font-family: 'Segoe UI', system-ui, sans-serif;
  max-width: 240px;
}

.crime-tooltip::before {
  border-top-color: #1a1d27 !important;
}

.marker-tooltip .tt-title {
  font-weight: 600;
  margin-bottom: 4px;
  color: #f1f5f9;
}

.marker-tooltip .tt-address {
  font-size: 0.8rem;
  color: #94a3b8;
  margin-bottom: 4px;
}

.marker-tooltip .tt-severity {
  font-size: 0.75rem;
  color: #a5b4fc;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
</style>
