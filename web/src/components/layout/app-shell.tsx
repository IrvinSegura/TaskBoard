import * as RadixDialog from '@radix-ui/react-dialog'
import { useState } from 'react'
import { Outlet } from 'react-router'
import { useNotificationStream } from '@/features/notifications/notification-queries'
import { useTeamDeletedStream } from '@/features/teams/team-queries'
import { Sidebar } from './sidebar'
import { Topbar } from './topbar'

/** Signed-in layout: sidebar (a drawer on small screens), top bar and the page. */
export function AppShell() {
  const [drawerOpen, setDrawerOpen] = useState(false)
  useNotificationStream()
  useTeamDeletedStream()

  return (
    <div className="flex h-full">
      <aside className="hidden w-64 shrink-0 border-r border-line bg-surface lg:block">
        <Sidebar />
      </aside>

      <RadixDialog.Root open={drawerOpen} onOpenChange={setDrawerOpen}>
        <RadixDialog.Portal>
          <RadixDialog.Overlay className="fixed inset-0 z-40 bg-black/40 data-[state=open]:animate-fade-in lg:hidden" />
          <RadixDialog.Content
            aria-describedby={undefined}
            className="fixed inset-y-0 left-0 z-50 w-72 border-r border-line bg-surface shadow-overlay focus:outline-none lg:hidden"
          >
            <RadixDialog.Title className="sr-only">Navegación</RadixDialog.Title>
            <Sidebar onNavigate={() => setDrawerOpen(false)} />
          </RadixDialog.Content>
        </RadixDialog.Portal>
      </RadixDialog.Root>

      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar onOpenSidebar={() => setDrawerOpen(true)} />
        <main className="scrollbar-thin min-h-0 flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
