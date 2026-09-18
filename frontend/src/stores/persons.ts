import { defineStore } from 'pinia'
import { ref } from 'vue'
import { personsApi } from '@/services/personsApi'
import type { PersonDto, PersonWriteDto } from '@/types/api'

/**
 * Pinia store for the person feature. Kept intentionally minimal for the map
 * MVP: the store only exposes a fetch-all, an autocomplete search, and a
 * create call. Persons are cross-cutting — victim in one event, witness in
 * another — so we do not tie the store lifetime to a specific event.
 */
export const usePersonsStore = defineStore('persons', () => {
  const persons = ref<PersonDto[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchAll(): Promise<void> {
    isLoading.value = true
    error.value = null
    try {
      const page = await personsApi.list({ pageSize: 200 })
      persons.value = page.items
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load persons.'
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function search(q: string): Promise<PersonDto[]> {
    const page = await personsApi.list({ q, pageSize: 20 })
    return page.items
  }

  async function create(body: PersonWriteDto): Promise<PersonDto> {
    const created = await personsApi.create(body)
    persons.value.push(created)
    return created
  }

  async function remove(id: string): Promise<void> {
    await personsApi.remove(id)
    persons.value = persons.value.filter((p) => p.id !== id)
  }

  return {
    persons,
    isLoading,
    error,
    fetchAll,
    search,
    create,
    remove,
  }
})
