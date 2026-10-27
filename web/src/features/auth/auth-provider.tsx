import { useQueryClient } from '@tanstack/react-query'
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { refreshSession } from '@/lib/api/client'
import { onSessionChange, setSession } from '@/lib/api/session'
import { authApi, type CurrentUser, type RegisterRequest } from './auth-api'

type AuthState =
  | { status: 'loading'; user: null }
  | { status: 'anonymous'; user: null }
  | { status: 'authenticated'; user: CurrentUser }

interface AuthContextValue {
  status: AuthState['status']
  user: CurrentUser | null
  signIn: (email: string, password: string) => Promise<void>
  signUp: (request: RegisterRequest) => Promise<void>
  signOut: () => Promise<void>
  /** Drops the local session without calling the API, e.g. after a password change revoked it. */
  endSession: () => void
  /** Reads the signed-in user again, e.g. after a profile change. */
  reloadUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

// Lets the other open tabs drop their session when this one signs out.
const channelName = 'taskboard-auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [state, setState] = useState<AuthState>({ status: 'loading', user: null })

  // A reload starts without an access token: trade the refresh cookie for one, if there is a cookie.
  useEffect(() => {
    let ignore = false
    void (async () => {
      const restored = await refreshSession()
      const user = restored ? await authApi.me().catch(() => null) : null
      if (!ignore) {
        setState(user ? { status: 'authenticated', user } : { status: 'anonymous', user: null })
      }
    })()
    return () => {
      ignore = true
    }
  }, [])

  // Any request can find out the session is gone (refresh rejected); go back to the sign-in screen then.
  useEffect(
    () =>
      onSessionChange((signedIn) => {
        if (!signedIn) {
          setState({ status: 'anonymous', user: null })
          queryClient.clear()
        }
      }),
    [queryClient],
  )

  useEffect(() => {
    if (typeof BroadcastChannel === 'undefined') {
      return
    }
    const channel = new BroadcastChannel(channelName)
    channel.onmessage = (event) => {
      if (event.data === 'signed-out') {
        setSession(null)
      }
    }
    return () => channel.close()
  }, [])

  const signIn = useCallback(async (email: string, password: string) => {
    setSession(await authApi.login(email, password))
    const user = await authApi.me()
    setState({ status: 'authenticated', user })
  }, [])

  const signUp = useCallback(
    async (request: RegisterRequest) => {
      await authApi.register(request)
      await signIn(request.email, request.password)
    },
    [signIn],
  )

  const reloadUser = useCallback(async () => {
    const user = await authApi.me()
    setState({ status: 'authenticated', user })
  }, [])

  const endSession = useCallback(() => {
    setSession(null)
    if (typeof BroadcastChannel !== 'undefined') {
      const channel = new BroadcastChannel(channelName)
      channel.postMessage('signed-out')
      channel.close()
    }
  }, [])

  const signOut = useCallback(async () => {
    try {
      await authApi.logout()
    } finally {
      endSession()
    }
  }, [endSession])

  const value = useMemo<AuthContextValue>(
    () => ({ status: state.status, user: state.user, signIn, signUp, signOut, endSession, reloadUser }),
    [state, signIn, signUp, signOut, endSession, reloadUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider')
  }
  return context
}

/** The signed-in user; only for components rendered behind RequireAuth. */
export function useCurrentUser(): CurrentUser {
  const { user } = useAuth()
  if (!user) {
    throw new Error('useCurrentUser needs a signed-in user')
  }
  return user
}
