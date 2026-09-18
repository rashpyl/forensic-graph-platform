import apiClient from './apiClient'
import type {
  PagedResult,
  PersonDto,
  PersonListQuery,
  PersonWriteDto,
} from '@/types/api'

/**
 * Thin wrapper around the /api/persons surface.
 * <c>list({ q })</c> doubles as the autocomplete backend for the map UI.
 */
export const personsApi = {
  async list(query: PersonListQuery = {}): Promise<PagedResult<PersonDto>> {
    const { data } = await apiClient.get<PagedResult<PersonDto>>('/api/persons', {
      params: query,
    })
    return data
  },

  async get(id: string): Promise<PersonDto> {
    const { data } = await apiClient.get<PersonDto>(`/api/persons/${id}`)
    return data
  },

  async create(body: PersonWriteDto): Promise<PersonDto> {
    const { data } = await apiClient.post<PersonDto>('/api/persons', body)
    return data
  },

  async update(id: string, body: PersonWriteDto): Promise<PersonDto> {
    const { data } = await apiClient.put<PersonDto>(`/api/persons/${id}`, body)
    return data
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/persons/${id}`)
  },
}
