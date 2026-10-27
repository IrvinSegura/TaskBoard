/**
 * Every React Query key in one place, so a feature can invalidate another one's data
 * (removing a team member also changes that team's boards) without importing its internals.
 */
export const queryKeys = {
  teams: () => ['teams'] as const,
  teamMembers: (teamId: string) => ['teams', teamId, 'members'] as const,
  teamBoards: (teamId: string, archived = false) => ['teams', teamId, 'boards', { archived }] as const,
  allTeamBoards: (teamId: string) => ['teams', teamId, 'boards'] as const,
  board: (boardId: string) => ['boards', boardId] as const,
  boardCards: (boardId: string, filters: object) => ['boards', boardId, 'cards', filters] as const,
  allBoardCards: (boardId: string) => ['boards', boardId, 'cards'] as const,
  card: (cardId: string) => ['cards', cardId] as const,
  cardComments: (cardId: string) => ['cards', cardId, 'comments'] as const,
  notifications: () => ['notifications'] as const,
  myCards: () => ['me', 'cards'] as const,
}
