import { ApiError } from './api-error'
import { api, buildUrl, refreshSession } from './client'
import { getSession, setSession } from './session'

function json(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

const freshTokens = (token: string) => ({
  accessToken: token,
  expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
})

describe('api client', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
    setSession(freshTokens('token-1'))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    setSession(null)
  })

  it('sends the bearer token and parses JSON', async () => {
    fetchMock.mockResolvedValueOnce(json(200, { id: 'me' }))

    const result = await api.get<{ id: string }>('/me')

    expect(result).toEqual({ id: 'me' })
    const [, init] = fetchMock.mock.calls[0]!
    expect((init!.headers as Record<string, string>).Authorization).toBe('Bearer token-1')
  })

  it('refreshes once and retries when the token is rejected', async () => {
    fetchMock
      .mockResolvedValueOnce(json(401))
      .mockResolvedValueOnce(json(200, freshTokens('token-2')))
      .mockResolvedValueOnce(json(200, []))

    await api.get('/teams')

    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(String(fetchMock.mock.calls[1]![0])).toContain('/auth/refresh')
    expect(fetchMock.mock.calls[1]![1]!.credentials).toBe('include')
    const [, retry] = fetchMock.mock.calls[2]!
    expect((retry!.headers as Record<string, string>).Authorization).toBe('Bearer token-2')
  })

  it('signs out when the refresh fails', async () => {
    fetchMock.mockResolvedValueOnce(json(401)).mockResolvedValueOnce(json(401))

    await expect(api.get('/teams')).rejects.toBeInstanceOf(ApiError)
    expect(getSession()).toBeNull()
  })

  it('shares one refresh request between concurrent callers', async () => {
    fetchMock.mockResolvedValue(json(200, freshTokens('token-3')))

    const results = await Promise.all([refreshSession(), refreshSession(), refreshSession()])

    expect(results).toEqual([true, true, true])
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('maps validation problems to field errors', async () => {
    fetchMock.mockResolvedValueOnce(
      json(400, {
        title: 'Validation.General',
        errors: [{ code: 'DisplayName', description: 'Display Name must not be empty.' }],
      }),
    )

    const error = await api.post('/teams', {}).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).fieldErrors).toEqual({ displayName: 'Display Name must not be empty.' })
  })

  it('uses Spanish copy for known error codes', async () => {
    fetchMock.mockResolvedValueOnce(json(401, { title: 'Users.InvalidCredentials' }))

    const error = await api.post('/auth/login', {}, { auth: false }).catch((e: unknown) => e)

    expect((error as ApiError).message).toBe('Email o contraseña incorrectos.')
  })

  it('skips empty query values', () => {
    expect(buildUrl('/boards/1/cards', { text: '', priority: 'High', page: 2, columnId: undefined })).toBe(
      'http://localhost:5080/boards/1/cards?priority=High&page=2',
    )
  })
})
