import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './auth-provider'

function FullScreenLoader() {
  return (
    <div className="flex h-full items-center justify-center" role="status" aria-label="Cargando sesión">
      <div className="flex flex-col items-center gap-4">
        <img src="/favicon.svg" alt="" className="size-10 animate-pulse" />
        <span className="text-sm text-muted">Cargando tu espacio…</span>
      </div>
    </div>
  )
}

export function RequireAuth() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return <FullScreenLoader />
  }
  if (status === 'anonymous') {
    const from = location.pathname + location.search
    return <Navigate to="/login" replace state={{ from }} />
  }
  return <Outlet />
}

export function RedirectIfAuthenticated() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return <FullScreenLoader />
  }
  if (status === 'authenticated') {
    const from = (location.state as { from?: string } | null)?.from
    return <Navigate to={from && from !== '/login' ? from : '/'} replace />
  }
  return <Outlet />
}
