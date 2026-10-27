import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useLocation, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { queryKeys } from '@/lib/api/query-keys'
import { useHubEvent } from '@/lib/realtime/realtime-provider'
import { teamsApi, type TeamRole, type TeamSummary } from './teams-api'

export function useTeams() {
  return useQuery({ queryKey: queryKeys.teams(), queryFn: teamsApi.list })
}

export function useTeam(teamId: string | undefined) {
  const teams = useTeams()
  return { ...teams, team: teams.data?.find((team) => team.id === teamId) }
}

export function useTeamMembers(teamId: string) {
  return useQuery({ queryKey: queryKeys.teamMembers(teamId), queryFn: () => teamsApi.members(teamId) })
}

export function useCreateTeam() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (name: string) => teamsApi.create(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.teams() }),
  })
}

export function useRenameTeam(teamId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (name: string) => teamsApi.rename(teamId, name),
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.teams() }),
  })
}

/** Drops the team from the cache right away, so the TeamDeleted message that follows finds nothing to announce. */
export function useDeleteTeam(teamId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => teamsApi.delete(teamId),
    onSuccess: () => {
      queryClient.setQueryData<TeamSummary[]>(queryKeys.teams(), (teams) => teams?.filter((t) => t.id !== teamId))
      queryClient.removeQueries({ queryKey: ['teams', teamId] })
    },
  })
}

interface TeamDeletedMessage {
  teamId: string
  name: string
}

/** Another member deleted a team: drop it from the sidebar and leave its pages. Mounted once, in the app shell. */
export function useTeamDeletedStream() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  useHubEvent<TeamDeletedMessage>('TeamDeleted', (message) => {
    const known = queryClient.getQueryData<TeamSummary[]>(queryKeys.teams())?.some((t) => t.id === message.teamId)
    void queryClient.invalidateQueries({ queryKey: queryKeys.teams() })
    queryClient.removeQueries({ queryKey: ['teams', message.teamId] })
    // Our own deletion already left the cache, so only other people's get a notice.
    if (known) {
      toast.warning(`Se eliminó el equipo «${message.name}»`)
    }
    if (pathname.startsWith(`/teams/${message.teamId}`)) {
      navigate('/teams', { replace: true })
    }
  })
}

/** Member changes also change the team's member count and who sees which board. */
function useTeamMemberMutation<TVariables>(teamId: string, mutationFn: (variables: TVariables) => Promise<void>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.teamMembers(teamId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.teams() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(teamId) }),
      ]),
  })
}

export function useAddTeamMember(teamId: string) {
  return useTeamMemberMutation(teamId, ({ email, role }: { email: string; role: TeamRole }) =>
    teamsApi.addMember(teamId, email, role),
  )
}

export function useChangeTeamRole(teamId: string) {
  return useTeamMemberMutation(teamId, ({ userId, role }: { userId: string; role: TeamRole }) =>
    teamsApi.changeRole(teamId, userId, role),
  )
}

export function useRemoveTeamMember(teamId: string) {
  return useTeamMemberMutation(teamId, (userId: string) => teamsApi.removeMember(teamId, userId))
}
