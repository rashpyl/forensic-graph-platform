import axios, { AxiosHeaders, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios'

/**
 * Shared axios instance.
 *
 * Authentication is an HttpOnly session cookie, which scripts cannot read, so an
 * XSS bug cannot steal it; `withCredentials` makes the browser send it.
 * State-changing requests also carry a CSRF token in `X-XSRF-TOKEN`. It is fetched
 * from `GET /api/auth/csrf`, kept in memory, and dropped on login/logout because
 * it is bound to the signed-in identity.
 * In development Vite proxies `/api` to the backend; `VITE_API_BASE_URL` overrides that.
 */
const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  timeout: 10_000,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

const UNSAFE_METHODS = new Set(['post', 'put', 'patch', 'delete'])
const CSRF_HEADER = 'X-XSRF-TOKEN'

let csrfToken: string | null = null
let csrfRequest: Promise<string> | null = null

async function getCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken
  if (!csrfRequest) {
    csrfRequest = axios
      .get<{ token: string }>(`${apiClient.defaults.baseURL}/api/auth/csrf`, { withCredentials: true })
      .then((res) => {
        csrfToken = res.data.token
        return csrfToken
      })
      .finally(() => {
        csrfRequest = null
      })
  }
  return csrfRequest
}

/** Forget the cached token; call after sign-in and sign-out. */
export function resetCsrfToken(): void {
  csrfToken = null
}

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  if (config.method && UNSAFE_METHODS.has(config.method.toLowerCase())) {
    const token = await getCsrfToken()
    config.headers = AxiosHeaders.from(config.headers)
    config.headers.set(CSRF_HEADER, token)
  }
  return config
})

type RetryableConfig = InternalAxiosRequestConfig & { _csrfRetried?: boolean }

/** Listeners notified when the server reports that the session is gone (401). */
const unauthorizedListeners = new Set<() => void>()

export function onUnauthorized(listener: () => void): () => void {
  unauthorizedListeners.add(listener)
  return () => unauthorizedListeners.delete(listener)
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (!axios.isAxiosError(error) || !error.response) {
      return Promise.reject(error)
    }

    const config = error.config as RetryableConfig | undefined
    const title = (error.response.data as { title?: string } | undefined)?.title ?? ''

    // A stale antiforgery token (e.g. the session changed in another tab):
    // fetch a fresh one and retry the request once.
    if (
      error.response.status === 400 &&
      title.toLowerCase().includes('antiforgery') &&
      config &&
      !config._csrfRetried
    ) {
      config._csrfRetried = true
      resetCsrfToken()
      return apiClient.request(config)
    }

    const isAuthProbe = config?.url?.includes('/api/auth/')
    if (error.response.status === 401 && !isAuthProbe) {
      resetCsrfToken()
      unauthorizedListeners.forEach((listener) => listener())
    }

    return Promise.reject(error)
  },
)

export default apiClient
