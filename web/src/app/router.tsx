import { createBrowserRouter } from 'react-router'
import { AppShell } from '@/components/layout/app-shell'
import { RedirectIfAuthenticated, RequireAuth } from '@/features/auth/route-guards'
import { boardView } from '@/features/boards/board-view'
import { RouteErrorPage } from './route-error-page'

// Each page is its own chunk, so the sign-in screen does not download the board (drag and drop) code.
export const router = createBrowserRouter([
  {
    errorElement: <RouteErrorPage />,
    children: [
      {
        element: <RedirectIfAuthenticated />,
        children: [
          {
            path: '/login',
            lazy: async () => ({ Component: (await import('@/features/auth/login-page')).LoginPage }),
          },
          {
            path: '/register',
            lazy: async () => ({ Component: (await import('@/features/auth/register-page')).RegisterPage }),
          },
        ],
      },
      {
        element: <RequireAuth />,
        children: [
          {
            element: <AppShell />,
            children: [
              {
                index: true,
                lazy: async () => ({ Component: (await import('@/features/dashboard/dashboard-page')).DashboardPage }),
              },
              {
                path: 'teams',
                lazy: async () => ({ Component: (await import('@/features/teams/teams-page')).TeamsPage }),
              },
              {
                path: 'teams/:teamId',
                lazy: async () => ({ Component: (await import('@/features/teams/team-layout')).TeamLayout }),
                children: [
                  {
                    index: true,
                    lazy: async () => ({
                      Component: (await import('@/features/boards/team-boards-panel')).TeamBoardsPanel,
                    }),
                  },
                  {
                    path: 'archived',
                    lazy: async () => ({
                      Component: (await import('@/features/boards/archived-boards-panel')).ArchivedBoardsPanel,
                    }),
                  },
                  {
                    path: 'members',
                    lazy: async () => ({
                      Component: (await import('@/features/teams/team-members-route')).TeamMembersRoute,
                    }),
                  },
                  {
                    path: 'activity',
                    lazy: async () => ({
                      Component: (await import('@/features/teams/team-activity-route')).TeamActivityRoute,
                    }),
                  },
                  {
                    path: 'settings',
                    lazy: async () => ({
                      Component: (await import('@/features/teams/team-settings-route')).TeamSettingsRoute,
                    }),
                  },
                ],
              },
              {
                path: 'boards/:boardId',
                lazy: async () => ({ Component: (await import('@/features/boards/board-page')).BoardPage }),
                children: [
                  {
                    path: 'cards/:cardId',
                    lazy: async () => ({
                      Component: (await import('@/features/cards/card-detail-route')).CardDetailRoute,
                    }),
                  },
                  {
                    path: 'archived',
                    lazy: async () => ({
                      Component: (await import('@/features/cards/archived-cards-route')).ArchivedCardsRoute,
                    }),
                  },
                  // Views that take the place of the canvas, marked so BoardPage knows (see boardView).
                  {
                    path: 'list',
                    handle: boardView,
                    lazy: async () => ({ Component: (await import('@/features/boards/board-list-view')).BoardListView }),
                  },
                  {
                    path: 'calendar',
                    handle: boardView,
                    lazy: async () => ({
                      Component: (await import('@/features/boards/board-calendar-view')).BoardCalendarView,
                    }),
                  },
                  {
                    path: 'activity',
                    handle: boardView,
                    lazy: async () => ({
                      Component: (await import('@/features/boards/board-activity-view')).BoardActivityView,
                    }),
                  },
                  {
                    path: 'settings',
                    handle: boardView,
                    lazy: async () => ({
                      Component: (await import('@/features/boards/board-settings-view')).BoardSettingsView,
                    }),
                  },
                ],
              },
              {
                path: 'chat',
                lazy: async () => ({ Component: (await import('@/features/chat/chat-page')).ChatPage }),
                children: [
                  {
                    path: ':conversationId',
                    lazy: async () => ({ Component: (await import('@/features/chat/chat-thread')).ChatThreadRoute }),
                  },
                ],
              },
              {
                path: 'my-tasks',
                lazy: async () => ({ Component: (await import('@/features/my-tasks/my-tasks-page')).MyTasksPage }),
              },
              {
                path: 'calendar',
                lazy: async () => ({ Component: (await import('@/features/calendar/calendar-page')).CalendarPage }),
              },
              {
                path: 'favorites',
                lazy: async () => ({ Component: (await import('@/features/favorites/favorites-page')).FavoritesPage }),
              },
              {
                path: 'activity',
                lazy: async () => ({ Component: (await import('@/features/activity/activity-page')).ActivityPage }),
              },
              {
                path: 'reports',
                lazy: async () => ({ Component: (await import('@/features/reports/reports-page')).ReportsPage }),
              },
              {
                path: 'archive',
                lazy: async () => ({ Component: (await import('@/features/archive/archive-page')).ArchivePage }),
              },
              {
                path: 'search',
                lazy: async () => ({ Component: (await import('@/features/search/search-page')).SearchPage }),
              },
              {
                path: 'help',
                lazy: async () => ({ Component: (await import('@/features/help/help-page')).HelpPage }),
              },
              {
                path: 'notifications',
                lazy: async () => ({
                  Component: (await import('@/features/notifications/notifications-page')).NotificationsPage,
                }),
              },
              {
                path: 'account',
                lazy: async () => ({ Component: (await import('@/features/account/account-page')).AccountPage }),
              },
              {
                path: '*',
                lazy: async () => ({ Component: (await import('./not-found-page')).NotFoundPage }),
              },
            ],
          },
        ],
      },
    ],
  },
])
