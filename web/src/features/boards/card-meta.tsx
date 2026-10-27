import { CalendarClock, ChevronDown, ChevronsUp, ChevronUp, Equal, Minus } from 'lucide-react'
import { Badge, type BadgeTone } from '@/components/ui/badge'
import { cn } from '@/lib/utils/cn'
import { dueStatus, formatShortDate } from '@/lib/utils/format'
import type { CardPriority } from './boards-api'

export const priorityMeta: Record<CardPriority, { label: string; tone: BadgeTone; icon: typeof Minus }> = {
  None: { label: 'Sin prioridad', tone: 'neutral', icon: Minus },
  Low: { label: 'Baja', tone: 'info', icon: ChevronDown },
  Medium: { label: 'Media', tone: 'warning', icon: Equal },
  High: { label: 'Alta', tone: 'orange', icon: ChevronUp },
  Urgent: { label: 'Urgente', tone: 'danger', icon: ChevronsUp },
}

export function PriorityBadge({ priority, className }: { priority: CardPriority; className?: string }) {
  if (priority === 'None') {
    return null
  }
  const { label, tone, icon: Icon } = priorityMeta[priority]
  return (
    <Badge tone={tone} className={className}>
      <Icon />
      {label}
    </Badge>
  )
}

const dueStyles = {
  overdue: 'bg-red-500/12 text-red-700 dark:text-red-400',
  soon: 'bg-amber-500/15 text-amber-700 dark:text-amber-400',
  later: 'bg-muted-surface text-muted',
}

export function DueDateChip({ dueDate, className }: { dueDate: string; className?: string }) {
  const status = dueStatus(dueDate)
  return (
    <span
      className={cn('inline-flex items-center gap-1 rounded-md px-1.5 py-0.5 text-xs font-medium', dueStyles[status], className)}
      title={status === 'overdue' ? 'Vencida' : status === 'soon' ? 'Vence pronto' : 'Fecha límite'}
    >
      <CalendarClock className="size-3" />
      {formatShortDate(dueDate)}
    </span>
  )
}
