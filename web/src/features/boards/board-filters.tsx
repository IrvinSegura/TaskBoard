import { Search, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Spinner } from '@/components/ui/spinner'
import type { BoardLabel } from '@/features/labels/labels-api'
import { cn } from '@/lib/utils/cn'
import { cardPriorities, type BoardMember, type CardPriority } from './boards-api'
import { priorityMeta } from './card-meta'
import { hasActiveFilters, emptyFilters, type CardFilters, type DueFilter } from './card-search'

interface BoardFiltersProps {
  filters: CardFilters
  onChange: (filters: CardFilters) => void
  members: BoardMember[]
  labels: BoardLabel[]
  meId: string
  searching: boolean
  canEdit: boolean
}

const compactSelect =
  'h-8 cursor-pointer rounded-lg border border-line bg-surface px-2.5 text-[13px] text-fg shadow-card hover:border-line-strong focus:border-primary focus:ring-3 focus:ring-ring focus:outline-none'

export function BoardFilters({ filters, onChange, members, labels, meId, searching, canEdit }: BoardFiltersProps) {
  const [text, setText] = useState(filters.text)

  // Debounced so typing does not send one search per key.
  useEffect(() => {
    if (text === filters.text) {
      return
    }
    const timer = setTimeout(() => onChange({ ...filters, text }), 300)
    return () => clearTimeout(timer)
  }, [text, filters, onChange])

  const active = hasActiveFilters(filters)

  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="relative">
        <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-faint" />
        <input
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder="Buscar tarjetas…"
          aria-label="Buscar tarjetas"
          className="h-8 w-56 rounded-lg border border-line bg-surface pr-8 pl-8 text-[13px] shadow-card placeholder:text-faint hover:border-line-strong focus:border-primary focus:ring-3 focus:ring-ring focus:outline-none"
        />
        {searching && <Spinner className="absolute top-1/2 right-2.5 size-3.5 -translate-y-1/2 text-faint" />}
      </div>

      <select
        aria-label="Filtrar por prioridad"
        value={filters.priority ?? ''}
        onChange={(event) => onChange({ ...filters, priority: (event.target.value || null) as CardPriority | null })}
        className={cn(compactSelect, filters.priority && 'border-primary text-primary')}
      >
        <option value="">Prioridad</option>
        {cardPriorities.map((priority) => (
          <option key={priority} value={priority}>
            {priorityMeta[priority].label}
          </option>
        ))}
      </select>

      <select
        aria-label="Filtrar por persona asignada"
        value={filters.assigneeId ?? ''}
        onChange={(event) => onChange({ ...filters, assigneeId: event.target.value || null })}
        className={cn(compactSelect, filters.assigneeId && 'border-primary text-primary')}
      >
        <option value="">Asignada a</option>
        <option value={meId}>Mí</option>
        {members
          .filter((member) => member.userId !== meId)
          .map((member) => (
            <option key={member.userId} value={member.userId}>
              {member.displayName}
            </option>
          ))}
      </select>

      {labels.length > 0 && (
        <select
          aria-label="Filtrar por etiqueta"
          value={filters.labelId ?? ''}
          onChange={(event) => onChange({ ...filters, labelId: event.target.value || null })}
          className={cn(compactSelect, filters.labelId && 'border-primary text-primary')}
        >
          <option value="">Etiqueta</option>
          {labels.map((label) => (
            <option key={label.id} value={label.id}>
              {label.name}
            </option>
          ))}
        </select>
      )}

      <select
        aria-label="Filtrar por fecha límite"
        value={filters.due ?? ''}
        onChange={(event) => onChange({ ...filters, due: (event.target.value || null) as DueFilter })}
        className={cn(compactSelect, filters.due && 'border-primary text-primary')}
      >
        <option value="">Fecha límite</option>
        <option value="overdue">Vencidas</option>
        <option value="week">Vencen en 7 días</option>
      </select>

      {active && (
        <button
          type="button"
          onClick={() => {
            setText('')
            onChange(emptyFilters)
          }}
          className="inline-flex h-8 cursor-pointer items-center gap-1 rounded-lg px-2 text-[13px] font-medium text-muted hover:bg-muted-surface hover:text-fg"
        >
          <X className="size-3.5" />
          Limpiar
        </button>
      )}
      {active && canEdit && (
        <span className="text-xs text-faint">Limpia los filtros para mover tarjetas.</span>
      )}
    </div>
  )
}
