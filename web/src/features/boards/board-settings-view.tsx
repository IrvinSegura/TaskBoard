import { BoardLabelsPanel } from '@/features/labels/board-labels-panel'
import { useBoardOutlet } from './board-page'

export function BoardSettingsView() {
  const { board, permissions } = useBoardOutlet()

  return (
    <div className="max-w-2xl space-y-6 px-4 py-6 sm:px-6">
      <div>
        <h2 className="text-[15px] font-semibold">Configuración</h2>
        <p className="mt-1 text-sm text-muted">Etiquetas, límites por columna y opciones del tablero.</p>
      </div>
      <BoardLabelsPanel boardId={board.id} labels={board.labels} canEdit={permissions.canEdit} />
    </div>
  )
}
