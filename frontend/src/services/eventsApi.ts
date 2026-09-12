import apiClient from './apiClient'
import type {
  CrimeEventDto,
  CrimeEventListQuery,
  CrimeEventWriteDto,
  EventRole,
  PagedResult,
} from '@/types/api'

/**
 * Thin wrapper around the /api/events surface. Every call returns the raw
 * DTOs from the backend — component code should not touch axios directly.
 */
export const eventsApi = {
  async list(query: CrimeEventListQuery = {}): Promise<PagedResult<CrimeEventDto>> {
    const { data } = await apiClient.get<PagedResult<CrimeEventDto>>('/api/events', {
      params: query,
    })
    return data
  },

  async get(id: string): Promise<CrimeEventDto> {
    const { data } = await apiClient.get<CrimeEventDto>(`/api/events/${id}`)
    return data
  },

  async create(body: CrimeEventWriteDto): Promise<CrimeEventDto> {
    const { data } = await apiClient.post<CrimeEventDto>('/api/events', body)
    return data
  },

  async update(id: string, body: CrimeEventWriteDto): Promise<CrimeEventDto> {
    const { data } = await apiClient.put<CrimeEventDto>(`/api/events/${id}`, body)
    return data
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/events/${id}`)
  },

  async assignPerson(eventId: string, personId: string, role: EventRole): Promise<void> {
    await apiClient.post(`/api/events/${eventId}/persons`, { personId, role })
  },

  async unassignPerson(eventId: string, personId: string, role: EventRole): Promise<void> {
    await apiClient.delete(`/api/events/${eventId}/persons/${personId}`, {
      params: { role },
    })
  },

  async link(fromId: string, toEventId: string, note?: string | null): Promise<void> {
    await apiClient.post(`/api/events/${fromId}/links`, { toEventId, note })
  },

  async unlink(fromId: string, toEventId: string): Promise<void> {
    await apiClient.delete(`/api/events/${fromId}/links/${toEventId}`)
  },
}
