import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { eventsApi } from '@/services/eventsApi'
import type { CrimeEventDto, CrimeEventListQuery, CrimeEventWriteDto } from '@/types/api'

/**
 * Pinia store for the crime-event feature. Owns the map's marker list, the
 * currently-selected event (loaded via GET /api/events/{id}, which returns
 * detail with persons + links), and status flags for the UI.
 *
 * All API calls are funnelled through <see cref="eventsApi"/> — components
 * never touch axios themselves.
 */
export const useEventsStore = defineStore('events', () => {
  const events = ref<CrimeEventDto[]>([])
  const selected = ref<CrimeEventDto | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const geolocatedEvents = computed(() =>
    events.value.filter(
      (e): e is CrimeEventDto & { latitude: number; longitude: number } =>
        typeof e.latitude === 'number' && typeof e.longitude === 'number',
    ),
  )

  async function fetchAll(query: CrimeEventListQuery = { pageSize: 200 }): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const page = await eventsApi.list(query)
      events.value = page.items
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load events.'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function select(id: string): Promise<void> {
    selected.value = await eventsApi.get(id)
  }

  function clearSelection(): void {
    selected.value = null
  }

  async function create(body: CrimeEventWriteDto): Promise<CrimeEventDto> {
    const created = await eventsApi.create(body)
    events.value.push(created)
    return created
  }

  async function remove(id: string): Promise<void> {
    await eventsApi.remove(id)
    events.value = events.value.filter((e) => e.id !== id)
    if (selected.value?.id === id) {
      selected.value = null
    }
  }

  return {
    events,
    selected,
    isLoading,
    error,
    geolocatedEvents,
    fetchAll,
    select,
    clearSelection,
    create,
    remove,
  }
})
