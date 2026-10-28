import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { BoardVisibility } from '@/features/boards/boards-api'
import type { TeamSummary } from '@/features/teams/teams-api'
import { errorMessage } from '@/lib/api/api-error'
import { api } from '@/lib/api/client'
import { queryKeys } from '@/lib/api/query-keys'

export interface FavoriteBoard {
  id: string
  name: string
  visibility: BoardVisibility
  teamId: string
  teamName: string
}

export interface Favorites {
  teams: TeamSummary[]
  boards: FavoriteBoard[]
}

export type FavoriteKind = 'board' | 'team'

const segments: Record<FavoriteKind, string> = { board: 'boards', team: 'teams' }

export const favoritesApi = {
  list: () => api.get<Favorites>('/me/favorites'),
  set: (kind: FavoriteKind, id: string, favorite: boolean) =>
    favorite ? api.put(`/me/favorites/${segments[kind]}/${id}`) : api.delete(`/me/favorites/${segments[kind]}/${id}`),
}

export function useFavorites() {
  return useQuery({ queryKey: queryKeys.favorites(), queryFn: favoritesApi.list })
}

/**
 * Whether a board or team is starred, and a toggle for it. While the request runs the star already
 * shows the new state (taken from the mutation's variables), and the list is reloaded once it settles.
 */
export function useFavorite(kind: FavoriteKind, id: string) {
  const queryClient = useQueryClient()
  const favorites = useFavorites()
  const mutation = useMutation({
    mutationFn: (favorite: boolean) => favoritesApi.set(kind, id, favorite),
    onSuccess: (_, favorite) => toast.success(favorite ? 'Añadido a favoritos' : 'Quitado de favoritos'),
    onError: (error) => toast.error(errorMessage(error)),
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.favorites() }),
  })

  const list: { id: string }[] = (kind === 'board' ? favorites.data?.boards : favorites.data?.teams) ?? []
  const stored = list.some((item) => item.id === id)
  const isFavorite = mutation.isPending ? mutation.variables : stored

  return {
    isFavorite,
    /** False until the list has loaded, so a click never stars something that is already starred. */
    ready: favorites.isSuccess,
    toggle: () => mutation.mutate(!isFavorite),
  }
}
