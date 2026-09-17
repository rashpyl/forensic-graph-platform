import { defineStore } from 'pinia'
import { ref } from 'vue'

/**
 * UI-only state that does not belong on the domain stores. Currently owns:
 *  - Location-picking mode for the map's "New Event" flow.
 *  - The pending coordinates handed from the map into <c>NewEventDialog</c>.
 *  - Open/closed flags for the two modal dialogs and the About modal.
 */
export const useUiStore = defineStore('ui', () => {
  const isPickingLocation = ref(false)
  const isNewEventDialogOpen = ref(false)
  const pendingLocation = ref<{ lat: number; lng: number } | null>(null)

  const isNewPersonDialogOpen = ref(false)
  const isAboutModalOpen = ref(false)

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

  function openNewPersonDialog(): void {
    isNewPersonDialogOpen.value = true
  }

  function closeNewPersonDialog(): void {
    isNewPersonDialogOpen.value = false
  }

  function openAboutModal(): void {
    isAboutModalOpen.value = true
  }

  function closeAboutModal(): void {
    isAboutModalOpen.value = false
  }

  return {
    isPickingLocation,
    isNewEventDialogOpen,
    pendingLocation,
    isNewPersonDialogOpen,
    isAboutModalOpen,
    startPickingLocation,
    cancelPicking,
    locationPicked,
    closeNewEventDialog,
    openNewPersonDialog,
    closeNewPersonDialog,
    openAboutModal,
    closeAboutModal,
  }
})
