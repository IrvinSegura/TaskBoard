import { UsersRound } from 'lucide-react'
import { Avatar } from '@/components/ui/avatar'
import { boardCover } from '@/features/boards/board-cover'
import { TeamAvatar } from '@/features/teams/team-avatar'
import { cn } from '@/lib/utils/cn'
import type { Conversation } from './chat-api'

const box = 'inline-flex size-10 shrink-0 items-center justify-center rounded-lg'

/** The other person in a direct chat, the team's or board's mark, or a group icon. All 40px. */
export function ConversationAvatar({ conversation, myId }: { conversation: Conversation; myId: string }) {
  switch (conversation.kind) {
    case 'Direct': {
      const other = conversation.participants.find((p) => p.id !== myId) ?? conversation.participants[0]
      return <Avatar id={other?.id} name={other?.displayName ?? conversation.title} size="lg" />
    }
    case 'Team':
      return <TeamAvatar id={conversation.teamId ?? conversation.id} name={conversation.title} size="md" />
    case 'Board':
      return (
        <span
          aria-hidden="true"
          className={cn(
            box,
            'bg-linear-to-br text-base font-semibold text-white shadow-card',
            boardCover(conversation.boardId ?? conversation.id),
          )}
        >
          #
        </span>
      )
    case 'Group':
      return (
        <span aria-hidden="true" className={cn(box, 'bg-primary-soft text-primary')}>
          <UsersRound className="size-5" />
        </span>
      )
  }
}
