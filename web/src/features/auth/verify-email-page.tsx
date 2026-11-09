import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { Alert } from '@/components/ui/alert'
import { authApi } from './auth-api'
import { AuthLayout } from './auth-layout'
import { useAuth } from './auth-provider'
import { readResetToken } from './reset-token'

type Status = 'verifying' | 'verified' | 'failed'

/** Opened from the welcome email: confirms the address with the token in the link. */
export function VerifyEmailPage() {
  const { status: session, reloadUser } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  // Kept here because the effect below takes it out of the address. Same fragment format as the reset link.
  const [token] = useState(() => readResetToken(location.hash))
  const [status, setStatus] = useState<Status>(token ? 'verifying' : 'failed')
  const sent = useRef(false)

  useEffect(() => {
    if (location.hash) {
      void navigate({ hash: '' }, { replace: true })
    }
  }, [location.hash, navigate])

  useEffect(() => {
    // Once, even when React runs effects twice in development.
    if (!token || sent.current) {
      return
    }
    sent.current = true
    authApi
      .verifyEmail(token)
      .then(() => setStatus('verified'))
      .catch(() => setStatus('failed'))
  }, [token])

  // A signed-in tab drops the "confirm your email" notice.
  useEffect(() => {
    if (status === 'verified' && session === 'authenticated') {
      void reloadUser()
    }
  }, [status, session, reloadUser])

  const next =
    session === 'authenticated' ? (
      <Link to="/" className="font-medium text-primary hover:underline">
        Ir a mis tableros
      </Link>
    ) : (
      <Link to="/login" className="font-medium text-primary hover:underline">
        Iniciar sesión
      </Link>
    )

  if (status === 'verifying') {
    return (
      <AuthLayout title="Confirmando tu correo…" subtitle="Esto tarda solo un momento.">
        <span className="sr-only" role="status">
          Confirmando
        </span>
      </AuthLayout>
    )
  }

  if (status === 'failed') {
    return (
      <AuthLayout title="Este enlace ya no sirve" subtitle={next}>
        <Alert>El enlace no es válido o ya venció. Pide otro desde Mi cuenta.</Alert>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Correo confirmado" subtitle={next}>
      <Alert tone="info">Listo: este correo ya está confirmado en tu cuenta de TaskBoard.</Alert>
    </AuthLayout>
  )
}
