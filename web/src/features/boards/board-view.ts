import { useMatches } from 'react-router'

/** Route handle of the board views that take the place of the canvas (list, calendar, activity, settings). */
export const boardView = { boardView: true } as const

export function useIsBoardView(): boolean {
  return useMatches().some((match) => (match.handle as Partial<typeof boardView> | undefined)?.boardView === true)
}
