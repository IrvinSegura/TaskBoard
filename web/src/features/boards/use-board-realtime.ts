import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { queryKeys } from '@/lib/api/query-keys'
import { useBoardChannel, useHubEvents } from '@/lib/realtime/realtime-provider'

// Events that change what GET /boards/{id} returns.
const boardEvents = [
  'BoardRenamed',
  'BoardArchived',
  'BoardRestored',
  'BoardMemberRemoved',
  'ColumnCreated',
  'ColumnRenamed',
  'ColumnMoved',
  'ColumnDeleted',
  'CardCreated',
  'CardUpdated',
  'CardMoved',
  'CardArchived',
  'CardRestored',
  'CardDeleted',
  'CardAssigned',
  'CardUnassigned',
  'LabelsChanged',
  'CardLabelsChanged',
  'ChecklistChanged',
] as const

// Events that only change one card's detail (comments are not part of the board view).
const cardEvents = ['CommentAdded', 'CommentEdited', 'CommentDeleted', 'AttachmentAdded', 'AttachmentDeleted'] as const

// Events after which the team's board lists (sidebar, archived tab) are stale too.
const teamListEvents = new Set(['BoardRenamed', 'BoardArchived', 'BoardRestored'])

interface BoardEventMessage {
  boardId?: string
  cardId?: string
  userId?: string
}

/**
 * Keeps an open board in sync with other people's changes. Events refetch the affected queries, batched
 * because one action can raise several events; the refetch also confirms our own optimistic updates.
 */
export function useBoardRealtime(boardId: string, teamId: string | undefined, enabled: boolean) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const me = useCurrentUser()
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)
  const pending = useRef({ board: false, teamLists: false, cards: new Set<string>() })
  const removed = useRef(false)

  useBoardChannel(boardId, enabled)

  useEffect(() => () => clearTimeout(timer.current), [])

  const flush = () => {
    const { board, teamLists, cards } = pending.current
    if (board) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId), exact: true })
      void queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(boardId) })
    }
    // Activity is written by the server after the change, so any board event may come with new lines.
    if (board || cards.size > 0) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.allActivity() })
    }
    if (teamLists && teamId) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(teamId) })
    }
    for (const cardId of cards) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.card(cardId) })
    }
    pending.current = { board: false, teamLists: false, cards: new Set() }
  }

  const schedule = (changes: { board?: boolean; teamLists?: boolean; cardId?: string }) => {
    pending.current.board ||= changes.board ?? false
    pending.current.teamLists ||= changes.teamLists ?? false
    if (changes.cardId) {
      pending.current.cards.add(changes.cardId)
    }
    clearTimeout(timer.current)
    timer.current = setTimeout(flush, 150)
  }

  useHubEvents<BoardEventMessage>(boardEvents, (message, event) => {
    if (event === 'BoardMemberRemoved' && message.boardId === boardId && message.userId === me.id) {
      // Can arrive twice (direct notice and group message), so only react once.
      if (!removed.current) {
        removed.current = true
        toast.warning('Ya no tienes acceso a este tablero')
        if (teamId) {
          void queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(teamId) })
        }
        navigate(teamId ? `/teams/${teamId}` : '/', { replace: true })
      }
      return
    }
    schedule({ board: true, teamLists: teamListEvents.has(event), cardId: message.cardId })
  })

  useHubEvents<BoardEventMessage>(cardEvents, (message) => schedule({ cardId: message.cardId }))

  useHubEvents<BoardEventMessage>(['BoardDeleted'], (message) => {
    if (message.boardId !== boardId || removed.current) {
      return
    }
    removed.current = true
    toast.warning('Este tablero se eliminó')
    queryClient.removeQueries({ queryKey: queryKeys.board(boardId) })
    if (teamId) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(teamId) })
    }
    navigate(teamId ? `/teams/${teamId}` : '/', { replace: true })
  })
}
