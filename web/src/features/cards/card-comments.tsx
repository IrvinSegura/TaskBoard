import { MessageSquare, Pencil, Trash2 } from 'lucide-react'
import { useState, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { ErrorState, Skeleton } from '@/components/ui/feedback'
import { useCurrentUser } from '@/features/auth/auth-provider'
import type { BoardMember } from '@/features/boards/boards-api'
import { errorMessage } from '@/lib/api/api-error'
import { formatRelative } from '@/lib/utils/format'
import { useAddComment, useCardComments, useDeleteComment, useEditComment } from './card-queries'
import type { CardComment } from './cards-api'
import { CommentBody } from './comment-body'
import { MentionTextarea } from './mention-textarea'
import { decodeMentions, encodeMentions, type Mention } from './mentions'
import { SectionTitle } from './section-title'

const maxLength = 4000

interface CardCommentsProps {
  cardId: string
  /** The board's members, who can be mentioned with "@". */
  members: BoardMember[]
  canEdit: boolean
  /** Board admins can delete anyone's comment. */
  canModerate: boolean
}

export function CardComments({ cardId, members, canEdit, canModerate }: CardCommentsProps) {
  const me = useCurrentUser()
  const comments = useCardComments(cardId)
  const addComment = useAddComment(cardId)
  const [body, setBody] = useState('')
  const [mentions, setMentions] = useState<Mention[]>([])
  const mentionable = members.filter((member) => member.userId !== me.id)

  const submit = () => {
    const trimmed = body.trim()
    if (!trimmed) {
      return
    }
    addComment.mutate(encodeMentions(trimmed, mentions), {
      onSuccess: () => {
        setBody('')
        setMentions([])
      },
      onError: (error) => toast.error(errorMessage(error)),
    })
  }

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && (event.metaKey || event.ctrlKey)) {
      submit()
    }
  }

  const all = comments.data?.pages.flatMap((page) => page.items) ?? []
  const total = comments.data?.pages[0]?.totalCount ?? 0

  return (
    <section>
      <SectionTitle icon={MessageSquare}>
        Comentarios
        {total > 0 && <span className="ml-1.5 font-normal text-faint">{total}</span>}
      </SectionTitle>

      {comments.isPending ? (
        <div className="space-y-3">
          <Skeleton className="h-14" />
          <Skeleton className="h-14" />
        </div>
      ) : comments.isError ? (
        <ErrorState error={comments.error} onRetry={() => comments.refetch()} className="py-6" />
      ) : (
        <>
          {all.length === 0 && <p className="mb-4 text-sm text-faint">Todavía no hay comentarios.</p>}
          <ol className="space-y-4">
            {all.map((comment) => (
              <CommentItem
                key={comment.id}
                cardId={cardId}
                comment={comment}
                members={mentionable}
                canEdit={canEdit && comment.authorId === me.id}
                canDelete={canEdit && (comment.authorId === me.id || canModerate)}
              />
            ))}
          </ol>
          {comments.hasNextPage && (
            <Button
              variant="ghost"
              size="sm"
              className="mt-3"
              loading={comments.isFetchingNextPage}
              onClick={() => void comments.fetchNextPage()}
            >
              Ver más comentarios
            </Button>
          )}
        </>
      )}

      {canEdit && (
        <div className="mt-5 flex gap-3">
          <Avatar id={me.id} name={me.displayName} />
          <div className="flex-1 space-y-2">
            <MentionTextarea
              value={body}
              onChange={setBody}
              mentions={mentions}
              onMentionsChange={setMentions}
              members={mentionable}
              maxLength={maxLength}
              onKeyDown={onKeyDown}
              placeholder="Escribe un comentario… usa @ para mencionar"
              aria-label="Nuevo comentario"
            />
            <div className="flex items-center gap-3">
              <Button size="sm" onClick={submit} disabled={!body.trim()} loading={addComment.isPending}>
                Comentar
              </Button>
              <span className="text-xs text-faint">Ctrl + Enter para enviar</span>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}

interface CommentItemProps {
  cardId: string
  comment: CardComment
  members: BoardMember[]
  canEdit: boolean
  canDelete: boolean
}

function CommentItem({ cardId, comment, members, canEdit, canDelete }: CommentItemProps) {
  const editComment = useEditComment(cardId)
  const deleteComment = useDeleteComment(cardId)
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState('')
  const [mentions, setMentions] = useState<Mention[]>([])

  const startEditing = () => {
    const decoded = decodeMentions(comment.body)
    setDraft(decoded.text)
    setMentions(decoded.mentions)
    setEditing(true)
  }

  const save = () => {
    const body = encodeMentions(draft.trim(), mentions)
    if (!body || body === comment.body) {
      setEditing(false)
      return
    }
    editComment.mutate(
      { commentId: comment.id, body },
      {
        onSuccess: () => setEditing(false),
        onError: (error) => toast.error(errorMessage(error)),
      },
    )
  }

  return (
    <li className="group flex gap-3">
      <Avatar id={comment.authorId} name={comment.authorName} />
      <div className="min-w-0 flex-1">
        <div className="flex items-baseline gap-2">
          <span className="text-sm font-semibold">{comment.authorName}</span>
          <time className="text-xs text-faint" dateTime={comment.createdAt} title={new Date(comment.createdAt).toLocaleString('es')}>
            {formatRelative(comment.createdAt)}
          </time>
          {comment.isEdited && <span className="text-xs text-faint">(editado)</span>}
          <div className="ml-auto flex gap-0.5 opacity-0 transition-opacity group-focus-within:opacity-100 group-hover:opacity-100">
            {canEdit && !editing && (
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label="Editar comentario"
                onClick={startEditing}
              >
                <Pencil />
              </Button>
            )}
            {canDelete && (
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label="Borrar comentario"
                className="hover:text-danger"
                loading={deleteComment.isPending}
                onClick={() =>
                  deleteComment.mutate(comment.id, { onError: (error) => toast.error(errorMessage(error)) })
                }
              >
                {!deleteComment.isPending && <Trash2 />}
              </Button>
            )}
          </div>
        </div>
        {editing ? (
          <div className="mt-1.5 space-y-2">
            <MentionTextarea
              autoFocus
              value={draft}
              onChange={setDraft}
              mentions={mentions}
              onMentionsChange={setMentions}
              members={members}
              maxLength={maxLength}
              onKeyDown={(event) => {
                if (event.key === 'Escape') {
                  event.stopPropagation()
                  setEditing(false)
                }
                if (event.key === 'Enter' && (event.metaKey || event.ctrlKey)) {
                  save()
                }
              }}
              aria-label="Editar comentario"
            />
            <div className="flex gap-2">
              <Button size="sm" onClick={save} loading={editComment.isPending}>
                Guardar
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setEditing(false)}>
                Cancelar
              </Button>
            </div>
          </div>
        ) : (
          <p className="mt-1 rounded-lg rounded-tl-sm bg-subtle px-3 py-2 text-sm leading-relaxed whitespace-pre-wrap break-words">
            <CommentBody body={comment.body} />
          </p>
        )}
      </div>
    </li>
  )
}
