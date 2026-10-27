import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/api/query-keys'
import { teamsApi, type TeamRole } from './teams-api'

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
