import axios, { type AxiosInstance } from 'axios'

/**
 * Shared axios instance. Base URL comes from VITE_API_BASE_URL so a production
 * build can point at a different backend without recompiling the frontend.
 * Local dev defaults to http://localhost:8080 via .env.development.
 */
const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080',
  timeout: 10_000,
  headers: {
    'Content-Type': 'application/json',
  },
})

export default apiClient
