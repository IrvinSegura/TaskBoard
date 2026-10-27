/**
 * The access token lives only in memory. The refresh token stays in an HttpOnly cookie the page cannot read,
 * so a reload starts without a token and asks POST /auth/refresh for a new one.
 */
export interface AccessTokenResponse {
  accessToken: string
  expiresAt: string
}

interface Session {
  accessToken: string
  expiresAt: number
}

type SessionListener = (signedIn: boolean) => void

let current: Session | null = null
const listeners = new Set<SessionListener>()

export function setSession(tokens: AccessTokenResponse | null): void {
  const wasSignedIn = current !== null
  current = tokens ? { accessToken: tokens.accessToken, expiresAt: Date.parse(tokens.expiresAt) } : null
  if (wasSignedIn !== (current !== null)) {
    listeners.forEach((listener) => listener(current !== null))
  }
}

export function getSession(): Session | null {
  return current
}

/** True when the token is missing or expires within `marginMs`, so callers refresh before using it. */
export function isSessionExpiring(marginMs = 30_000): boolean {
  return current === null || current.expiresAt - Date.now() <= marginMs
}

export function onSessionChange(listener: SessionListener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}
