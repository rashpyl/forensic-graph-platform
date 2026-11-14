import apiClient from './apiClient'
import type {
  AuditEntryDto,
  AuditQuery,
  CreateUserRequest,
  PagedResult,
  UserDto,
  UserRole,
} from '@/types/api'

/** Admin-only endpoints: user accounts and the audit log. */
export const usersApi = {
  async list(): Promise<UserDto[]> {
    const { data } = await apiClient.get<UserDto[]>('/api/users')
    return data
  },

  async create(body: CreateUserRequest): Promise<UserDto> {
    const { data } = await apiClient.post<UserDto>('/api/users', body)
    return data
  },

  async changeRole(id: string, role: UserRole): Promise<UserDto> {
    const { data } = await apiClient.put<UserDto>(`/api/users/${id}/role`, { role })
    return data
  },

  async deactivate(id: string): Promise<UserDto> {
    const { data } = await apiClient.post<UserDto>(`/api/users/${id}/deactivate`)
    return data
  },

  async activate(id: string): Promise<UserDto> {
    const { data } = await apiClient.post<UserDto>(`/api/users/${id}/activate`)
    return data
  },

  async resetPassword(id: string, newPassword: string): Promise<void> {
    await apiClient.post(`/api/users/${id}/reset-password`, { newPassword })
  },
}

export const auditApi = {
  async list(query: AuditQuery = {}): Promise<PagedResult<AuditEntryDto>> {
    const { data } = await apiClient.get<PagedResult<AuditEntryDto>>('/api/audit', { params: query })
    return data
  },
}
