import { ApiError, NetworkError, type ProblemDetails } from './api-error'
import { getSession, isSessionExpiring, setSession, type AccessTokenResponse } from './session'

export const API_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5080').replace(/\/$/, '')

type QueryValue = string | number | boolean | null | undefined

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  query?: Record<string, QueryValue>
  signal?: AbortSignal
  /** False for /auth/* calls, which never send the bearer token. */
  auth?: boolean
}

export function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(API_URL + path)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== '') {
      url.searchParams.set(key, String(value))
    }
  }
  return url.toString()
}

let refreshInFlight: Promise<boolean> | null = null

async function callRefresh(): Promise<boolean> {
  try {
    // The cookie is scoped to /auth and only travels with credentials: 'include'.
    const response = await fetch(buildUrl('/auth/refresh'), { method: 'POST', credentials: 'include' })
    if (!response.ok) {
      setSession(null)
      return false
    }
    setSession((await response.json()) as AccessTokenResponse)
    return true
  } catch {
    // Offline is not a logout: keep whatever session we had and let the caller fail.
    return getSession() !== null
  }
}

/**
 * Swaps the refresh cookie for a new access token. Calls in this tab share one request, and the Web Locks API
 * keeps other tabs from refreshing at the same moment: two refreshes with the same cookie count as token reuse
 * and the API would close every session of the user.
 */
export function refreshSession(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      if (typeof navigator !== 'undefined' && navigator.locks) {
        return await navigator.locks.request('taskboard-refresh', callRefresh)
      }
      return await callRefresh()
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

/** A token that is valid for at least the next 30 seconds, or null when the user is signed out. */
export async function getAccessToken(): Promise<string | null> {
  if (isSessionExpiring() && getSession() !== null) {
    await refreshSession()
  }
  return getSession()?.accessToken ?? null
}

async function send(path: string, options: RequestOptions, token: string | null): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  let body: BodyInit | undefined
  if (options.body instanceof FormData) {
    // The browser sets the multipart boundary itself.
    body = options.body
  } else if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
    body = JSON.stringify(options.body)
  }
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  try {
    return await fetch(buildUrl(path, options.query), {
      method: options.method ?? 'GET',
      headers,
      body,
      signal: options.signal,
      credentials: path.startsWith('/auth') ? 'include' : 'same-origin',
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }
    throw new NetworkError()
  }
}

async function toError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | undefined
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    problem = undefined
  }
  return new ApiError(response.status, problem)
}

/** Sends the request, refreshing the session once and retrying when the access token was rejected. */
export async function requestRaw(path: string, options: RequestOptions = {}): Promise<Response> {
  const authenticated = options.auth ?? true
  let response = await send(path, options, authenticated ? await getAccessToken() : null)

  if (response.status === 401 && authenticated && (await refreshSession())) {
    response = await send(path, options, getSession()?.accessToken ?? null)
  }
  if (response.status === 401 && authenticated) {
    setSession(null)
  }
  if (!response.ok) {
    throw await toError(response)
  }
  return response
}

export async function request<T = void>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await requestRaw(path, options)
  if (response.status === 204 || response.headers.get('Content-Length') === '0') {
    return undefined as T
  }
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const api = {
  get: <T>(path: string, options?: Omit<RequestOptions, 'method' | 'body'>) => request<T>(path, options),
  post: <T = void>(path: string, body?: unknown, options?: Omit<RequestOptions, 'method' | 'body'>) =>
    request<T>(path, { ...options, method: 'POST', body }),
  put: <T = void>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  patch: <T = void>(path: string, body?: unknown) => request<T>(path, { method: 'PATCH', body }),
  delete: <T = void>(path: string) => request<T>(path, { method: 'DELETE' }),
}

/** The paged shape of /boards/{id}/cards and /cards/{id}/comments. */
export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  hasNextPage: boolean
}

export interface CreatedResponse {
  id: string
}
