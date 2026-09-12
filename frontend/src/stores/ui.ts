import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * UI-only state that does not belong on the domain stores. Currently owns:
 *  - Location-picking mode: when the user clicks "New Event" in the navbar,
 *    <see cref="isPickingLocation"/> flips true and the map view swaps its
 *    cursor + click handler to capture a lat/lng pair.
 *  - The pending coordinates passed from the map into the new-event dialog.
 *  - Whether the new-event dialog is currently open.
 */
export const useUiStore = defineStore('ui', () => {
  const isPickingLocation = ref(false)
  const isNewEventDialogOpen = ref(false)
  const pendingLocation = ref<{ lat: number; lng: number } | null>(null)

  function startPickingLocation(): void {
    isPickingLocation.value = true
  }

  function cancelPicking(): void {
    isPickingLocation.value = false
    pendingLocation.value = null
  }

  function locationPicked(lat: number, lng: number): void {
    pendingLocation.value = { lat, lng }
    isPickingLocation.value = false
    isNewEventDialogOpen.value = true
  }

  function closeNewEventDialog(): void {
    isNewEventDialogOpen.value = false
    pendingLocation.value = null
  }

  return {
    isPickingLocation,
    isNewEventDialogOpen,
    pendingLocation,
    startPickingLocation,
    cancelPicking,
    locationPicked,
    closeNewEventDialog,
  }
})
