import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
} from '@microsoft/signalr'
import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import { API_URL, getAccessToken } from '@/lib/api/client'

interface RealtimeContextValue {
  connection: HubConnection | null
  state: HubConnectionState
}

const RealtimeContext = createContext<RealtimeContextValue>({
  connection: null,
  state: HubConnectionState.Disconnected,
})

// withAutomaticReconnect only covers dropped connections, not a first start that fails (API still booting).
const startRetryDelaysMs = [0, 2_000, 5_000, 10_000, 30_000]

/** One SignalR connection per signed-in tab, shared by every board and the notifications. */
export function RealtimeProvider({ enabled, children }: { enabled: boolean; children: ReactNode }) {
  const [value, setValue] = useState<RealtimeContextValue>({
    connection: null,
    state: HubConnectionState.Disconnected,
  })

  useEffect(() => {
    if (!enabled) {
      return
    }
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/board`, {
        // Browsers cannot put headers on WebSockets; SignalR sends this as ?access_token=.
        accessTokenFactory: async () => (await getAccessToken()) ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    let stopped = false
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    const publish = () => !stopped && setValue({ connection, state: connection.state })

    connection.onreconnecting(publish)
    connection.onreconnected(publish)
    connection.onclose(publish)

    const start = async (attempt: number) => {
      try {
        await connection.start()
        publish()
      } catch {
        if (!stopped) {
          const delay = startRetryDelaysMs[Math.min(attempt + 1, startRetryDelaysMs.length - 1)]
          retryTimer = setTimeout(() => void start(attempt + 1), delay)
        }
      }
    }
    publish()
    void start(0)

    return () => {
      stopped = true
      clearTimeout(retryTimer)
      void connection.stop()
      setValue({ connection: null, state: HubConnectionState.Disconnected })
    }
  }, [enabled])

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>
}

export function useRealtime(): RealtimeContextValue {
  return useContext(RealtimeContext)
}

/**
 * Subscribes to server events while the component is mounted. The handler gets the event name too,
 * and can change between renders without resubscribing.
 */
export function useHubEvents<T>(events: readonly string[], handler: (message: T, event: string) => void): void {
  const { connection } = useRealtime()
  const handlerRef = useRef(handler)
  const eventsKey = events.join(',')

  useEffect(() => {
    handlerRef.current = handler
  })

  useEffect(() => {
    if (!connection) {
      return
    }
    const subscriptions = eventsKey.split(',').map((event) => {
      const listener = (message: T) => handlerRef.current(message, event)
      connection.on(event, listener)
      return () => connection.off(event, listener)
    })
    return () => subscriptions.forEach((unsubscribe) => unsubscribe())
  }, [connection, eventsKey])
}

export function useHubEvent<T>(event: string, handler: (message: T) => void): void {
  useHubEvents<T>([event], handler)
}

/** Joins the board's hub group while mounted, and again after every reconnect (groups do not survive one). */
export function useBoardChannel(boardId: string, enabled = true): void {
  const { connection, state } = useRealtime()

  useEffect(() => {
    if (!connection || !enabled || state !== HubConnectionState.Connected) {
      return
    }
    // Only members can join; a refusal just means no live updates, the page still works.
    connection.invoke('JoinBoard', boardId).catch(() => undefined)
    return () => {
      if (connection.state === HubConnectionState.Connected) {
        connection.invoke('LeaveBoard', boardId).catch(() => undefined)
      }
    }
  }, [connection, state, boardId, enabled])
}
