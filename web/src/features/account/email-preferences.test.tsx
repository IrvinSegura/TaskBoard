import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { EmailPreferences } from './email-preferences'
import { emailPreferencesApi } from './email-preferences-api'

vi.mock('./email-preferences-api', () => ({
  emailPreferencesApi: { get: vi.fn(), update: vi.fn() },
}))
vi.mock('sonner', () => ({ toast: { error: vi.fn() } }))

function renderPreferences() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <EmailPreferences />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('EmailPreferences', () => {
  it('shows a switch per notification, on unless it is muted', async () => {
    vi.mocked(emailPreferencesApi.get).mockResolvedValue({ mutedTypes: ['CommentAdded'] })

    renderPreferences()

    expect(await screen.findByRole('switch', { name: 'Comentarios' })).not.toBeChecked()
    expect(screen.getByRole('switch', { name: 'Tarjetas asignadas' })).toBeChecked()
    expect(screen.getAllByRole('switch')).toHaveLength(9)
  })

  it('mutes a notification when its switch is turned off', async () => {
    vi.mocked(emailPreferencesApi.get).mockResolvedValue({ mutedTypes: ['CommentAdded'] })
    vi.mocked(emailPreferencesApi.update).mockResolvedValue(undefined as never)
    const user = userEvent.setup()
    renderPreferences()

    await user.click(await screen.findByRole('switch', { name: 'Menciones' }))

    await waitFor(() => expect(emailPreferencesApi.update).toHaveBeenCalledWith(['CommentAdded', 'Mentioned']))
  })

  it('unmutes a notification when its switch is turned on', async () => {
    vi.mocked(emailPreferencesApi.get).mockResolvedValue({ mutedTypes: ['CommentAdded', 'Mentioned'] })
    vi.mocked(emailPreferencesApi.update).mockResolvedValue(undefined as never)
    const user = userEvent.setup()
    renderPreferences()

    await user.click(await screen.findByRole('switch', { name: 'Comentarios' }))

    await waitFor(() => expect(emailPreferencesApi.update).toHaveBeenCalledWith(['Mentioned']))
  })

  it('puts the switch back when saving fails', async () => {
    vi.mocked(emailPreferencesApi.get).mockResolvedValue({ mutedTypes: [] })
    vi.mocked(emailPreferencesApi.update).mockRejectedValue(new Error('Sin conexión'))
    const user = userEvent.setup()
    renderPreferences()

    const toggle = await screen.findByRole('switch', { name: 'Equipos nuevos' })
    await user.click(toggle)

    await waitFor(() => expect(emailPreferencesApi.update).toHaveBeenCalled())
    await waitFor(() => expect(screen.getByRole('switch', { name: 'Equipos nuevos' })).toBeChecked())
  })
})
