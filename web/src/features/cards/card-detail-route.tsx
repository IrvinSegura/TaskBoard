import * as RadixDialog from '@radix-ui/react-dialog'
import { Archive, ArchiveRestore, X } from 'lucide-react'
import { useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { ErrorState, Skeleton } from '@/components/ui/feedback'
import { useBoardOutlet } from '@/features/boards/board-page'
import { errorMessage } from '@/lib/api/api-error'
import { formatLongDate } from '@/lib/utils/format'
import { CardAttachments } from './card-attachments'
import { CardComments } from './card-comments'
import { CardDescriptionEditor } from './card-description-editor'
import { CardFields } from './card-fields'
import { useArchiveCard, useCard, useRestoreCard } from './card-queries'
import { CardTitleEditor } from './card-title-editor'

/** The card detail, rendered over the board as /boards/:boardId/cards/:cardId so it can be linked. */
export function CardDetailRoute() {
  const { cardId = '' } = useParams<{ cardId: string }>()
  const navigate = useNavigate()
  const { board, permissions } = useBoardOutlet()
  const card = useCard(cardId)
  const archive = useArchiveCard(cardId, board.id)
  const restore = useRestoreCard(cardId, board.id)

  const close = () => navigate(`/boards/${board.id}`)
  const canEdit = permissions.canEdit && !card.data?.isArchived
  const column = board.columns.find((c) => c.id === card.data?.columnId)
  const creator = board.members.find((m) => m.userId === card.data?.createdById)

  return (
    <RadixDialog.Root open onOpenChange={(open) => !open && close()}>
      <RadixDialog.Portal>
        <RadixDialog.Overlay className="fixed inset-0 z-40 bg-black/40 backdrop-blur-[2px] data-[state=open]:animate-fade-in" />
        <RadixDialog.Content
          aria-describedby={undefined}
          className="fixed top-1/2 left-1/2 z-40 flex max-h-[calc(100dvh-2rem)] w-[calc(100vw-2rem)] max-w-4xl -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-2xl border border-line bg-surface shadow-overlay focus:outline-none data-[state=open]:animate-scale-in"
        >
          {card.isPending ? (
            <>
              <RadixDialog.Title className="sr-only">Cargando tarjeta</RadixDialog.Title>
              <div className="space-y-4 p-6">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-8 w-2/3" />
                <Skeleton className="h-32" />
              </div>
            </>
          ) : card.isError ? (
            <>
              <RadixDialog.Title className="sr-only">Error</RadixDialog.Title>
              <ErrorState error={card.error} onRetry={() => card.refetch()} className="m-6 border-0" />
            </>
          ) : (
            <>
              <div className="flex items-start gap-3 border-b border-line px-6 pt-5 pb-4">
                <div className="min-w-0 flex-1">
                  <p className="text-[13px] text-muted">
                    en <span className="font-medium text-fg">{column?.name ?? 'una columna'}</span>
                  </p>
                  <RadixDialog.Title asChild>
                    <div>
                      <CardTitleEditor card={card.data} canEdit={canEdit} />
                    </div>
                  </RadixDialog.Title>
                </div>
                <RadixDialog.Close
                  className="-mr-2 cursor-pointer rounded-md p-1.5 text-faint transition-colors hover:bg-muted-surface hover:text-fg"
                  aria-label="Cerrar"
                >
                  <X className="size-5" />
                </RadixDialog.Close>
              </div>

              <div className="scrollbar-thin grid min-h-0 flex-1 overflow-y-auto md:grid-cols-[minmax(0,1fr)_16rem]">
                <div className="space-y-8 px-6 py-5">
                  {card.data.isArchived && (
                    <Alert tone="warning">Esta tarjeta está archivada. Restáurala para editarla.</Alert>
                  )}
                  <CardDescriptionEditor card={card.data} canEdit={canEdit} />
                  <CardAttachments card={card.data} members={board.members} canEdit={canEdit} />
                  <CardComments cardId={card.data.id} canEdit={canEdit} canModerate={permissions.canAdmin} />
                </div>

                <aside className="space-y-6 border-line bg-subtle/60 px-5 py-5 md:border-l">
                  <CardFields card={card.data} board={board} canEdit={canEdit} />

                  {permissions.canEdit && (
                    <div className="border-t border-line pt-5">
                      {card.data.isArchived ? (
                        <Button
                          variant="secondary"
                          className="w-full"
                          loading={restore.isPending}
                          onClick={() =>
                            restore.mutate(undefined, {
                              onSuccess: () => toast.success('Tarjeta restaurada'),
                              onError: (error) => toast.error(errorMessage(error)),
                            })
                          }
                        >
                          <ArchiveRestore />
                          Restaurar tarjeta
                        </Button>
                      ) : (
                        <Button
                          variant="secondary"
                          className="w-full"
                          loading={archive.isPending}
                          onClick={() =>
                            archive.mutate(undefined, {
                              onSuccess: () => {
                                toast.success('Tarjeta archivada', {
                                  action: { label: 'Deshacer', onClick: () => restore.mutate(undefined) },
                                })
                                close()
                              },
                              onError: (error) => toast.error(errorMessage(error)),
                            })
                          }
                        >
                          <Archive />
                          Archivar tarjeta
                        </Button>
                      )}
                    </div>
                  )}

                  <p className="text-xs leading-relaxed text-faint">
                    Creada el {formatLongDate(card.data.createdAt)}
                    {creator && ` por ${creator.displayName}`}
                  </p>
                </aside>
              </div>
            </>
          )}
        </RadixDialog.Content>
      </RadixDialog.Portal>
    </RadixDialog.Root>
  )
}
