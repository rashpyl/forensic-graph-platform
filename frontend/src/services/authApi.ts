import apiClient, { resetCsrfToken } from './apiClient'
import type { CurrentUserDto } from '@/types/api'

export const authApi = {
  async login(email: string, password: string): Promise<CurrentUserDto> {
    const { data } = await apiClient.post<CurrentUserDto>('/api/auth/login', { email, password })
    // The antiforgery token is bound to the identity; the anonymous one is now invalid.
    resetCsrfToken()
    return data
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/api/auth/logout')
    } finally {
      resetCsrfToken()
    }
  },

  async me(): Promise<CurrentUserDto> {
    const { data } = await apiClient.get<CurrentUserDto>('/api/auth/me')
    return data
  },
}
