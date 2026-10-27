import { createBrowserRouter } from 'react-router'
import { AppShell } from '@/components/layout/app-shell'
import { RedirectIfAuthenticated, RequireAuth } from '@/features/auth/route-guards'
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
                ],
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
