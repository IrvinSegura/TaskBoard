import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { RouterProvider } from 'react-router'
import { Toaster } from 'sonner'
import { AuthProvider, useAuth } from '@/features/auth/auth-provider'
import { ApiError } from '@/lib/api/api-error'
import { RealtimeProvider } from '@/lib/realtime/realtime-provider'
import { ThemeProvider, useTheme } from '@/lib/theme/theme-provider'
import { router } from './router'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // 4xx answers will not change on retry (not found, no access...); only retry network and server errors.
      retry: (failureCount, error) =>
        failureCount < 2 && !(error instanceof ApiError && error.status >= 400 && error.status < 500),
    },
  },
})

function AppRealtime({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  return <RealtimeProvider enabled={status === 'authenticated'}>{children}</RealtimeProvider>
}

function ThemedToaster() {
  const { resolved } = useTheme()
  return <Toaster theme={resolved} position="bottom-right" richColors closeButton />
}

export function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <AppRealtime>
            <RouterProvider router={router} />
          </AppRealtime>
        </AuthProvider>
      </QueryClientProvider>
      <ThemedToaster />
    </ThemeProvider>
  )
}
