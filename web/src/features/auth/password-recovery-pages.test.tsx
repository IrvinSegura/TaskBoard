import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/lib/api/api-error'
import { authApi } from './auth-api'
import { ForgotPasswordPage } from './forgot-password-page'
import { ResetPasswordPage } from './reset-password-page'

const endSession = vi.fn()

vi.mock('./auth-api', () => ({
  authApi: { forgotPassword: vi.fn(), resetPassword: vi.fn() },
}))
vi.mock('./auth-provider', () => ({
  useAuth: () => ({ endSession }),
}))

function LoginStub() {
  const location = useLocation()
  return <p>Login: {(location.state as { notice: string }).notice}</p>
}

function AddressProbe() {
  const location = useLocation()
  return <output aria-label="address">{location.pathname + location.hash}</output>
}

function renderReset(entry: string) {
  render(
    <MemoryRouter initialEntries={[entry]}>
      <AddressProbe />
      <Routes>
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/forgot-password" element={<p>Forgot page</p>} />
        <Route path="/login" element={<LoginStub />} />
      </Routes>
    </MemoryRouter>,
  )
}

async function fillPasswords(user: ReturnType<typeof userEvent.setup>, password: string, confirmation = password) {
  await user.type(screen.getByLabelText('Nueva contraseña'), password)
  await user.type(screen.getByLabelText('Confirmar nueva contraseña'), confirmation)
  await user.click(screen.getByRole('button', { name: 'Guardar contraseña' }))
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('ForgotPasswordPage', () => {
  function renderForgot() {
    render(
      <MemoryRouter>
        <ForgotPasswordPage />
      </MemoryRouter>,
    )
  }

  it('asks the API and answers without saying whether the account exists', async () => {
    vi.mocked(authApi.forgotPassword).mockResolvedValue(undefined as never)
    const user = userEvent.setup()
    renderForgot()

    await user.type(screen.getByLabelText('Email'), 'ana@example.com')
    await user.click(screen.getByRole('button', { name: 'Enviar enlace' }))

    expect(authApi.forgotPassword).toHaveBeenCalledWith('ana@example.com')
    expect(await screen.findByText(/Si hay una cuenta con ese email/)).toBeInTheDocument()
  })

  it('does not call the API with an invalid email', async () => {
    const user = userEvent.setup()
    renderForgot()

    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.click(screen.getByRole('button', { name: 'Enviar enlace' }))

    expect(await screen.findByText('Escribe un email válido.')).toBeInTheDocument()
    expect(authApi.forgotPassword).not.toHaveBeenCalled()
  })

  it('shows the error when the request fails', async () => {
    vi.mocked(authApi.forgotPassword).mockRejectedValue(new ApiError(429))
    const user = userEvent.setup()
    renderForgot()

    await user.type(screen.getByLabelText('Email'), 'ana@example.com')
    await user.click(screen.getByRole('button', { name: 'Enviar enlace' }))

    expect(await screen.findByText(/Demasiados intentos/)).toBeInTheDocument()
    expect(screen.queryByText(/Si hay una cuenta/)).not.toBeInTheDocument()
  })
})

describe('ResetPasswordPage', () => {
  it('takes the token out of the address once it has read it', async () => {
    renderReset('/reset-password#token=abc')

    await waitFor(() => expect(screen.getByLabelText('address')).toHaveTextContent(/^\/reset-password$/))
    expect(screen.getByRole('button', { name: 'Guardar contraseña' })).toBeInTheDocument()
  })

  it('sends the token with the new password, ends the session and goes to the sign-in page', async () => {
    vi.mocked(authApi.resetPassword).mockResolvedValue(undefined as never)
    const user = userEvent.setup()
    renderReset('/reset-password#token=abc')

    await fillPasswords(user, 'new-password-1')

    expect(await screen.findByText(/Login: Contraseña actualizada/)).toBeInTheDocument()
    expect(authApi.resetPassword).toHaveBeenCalledWith('abc', 'new-password-1')
    expect(endSession).toHaveBeenCalledOnce()
  })

  it('needs both passwords to match and be long enough', async () => {
    const user = userEvent.setup()
    renderReset('/reset-password#token=abc')

    await fillPasswords(user, 'new-password-1', 'different-password')
    expect(await screen.findByText('Las contraseñas no coinciden.')).toBeInTheDocument()

    await user.clear(screen.getByLabelText('Nueva contraseña'))
    await user.type(screen.getByLabelText('Nueva contraseña'), 'short')
    await user.click(screen.getByRole('button', { name: 'Guardar contraseña' }))
    expect(await screen.findByText(/al menos 8 caracteres/)).toBeInTheDocument()

    expect(authApi.resetPassword).not.toHaveBeenCalled()
  })

  it('offers a new link when the address has no token', () => {
    renderReset('/reset-password')

    expect(screen.getByText(/no es válido, ya venció o ya se usó/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Pide un enlace nuevo' })).toHaveAttribute('href', '/forgot-password')
    expect(screen.queryByLabelText('Nueva contraseña')).not.toBeInTheDocument()
  })

  it('offers a new link when the API rejects the token', async () => {
    vi.mocked(authApi.resetPassword).mockRejectedValue(new ApiError(400, { title: 'Users.InvalidPasswordResetToken' }))
    const user = userEvent.setup()
    renderReset('/reset-password#token=old')

    await fillPasswords(user, 'new-password-1')

    expect(await screen.findByRole('link', { name: 'Pide un enlace nuevo' })).toBeInTheDocument()
    expect(endSession).not.toHaveBeenCalled()
  })

  it('keeps the form when the failure has nothing to do with the link', async () => {
    vi.mocked(authApi.resetPassword).mockRejectedValue(new ApiError(500))
    const user = userEvent.setup()
    renderReset('/reset-password#token=abc')

    await fillPasswords(user, 'new-password-1')

    expect(await screen.findByText(/Algo salió mal/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Guardar contraseña' })).toBeInTheDocument()
  })
})
