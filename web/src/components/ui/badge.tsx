import type { HTMLAttributes } from 'react'
import { cn } from '@/lib/utils/cn'

export type BadgeTone = 'neutral' | 'primary' | 'success' | 'warning' | 'orange' | 'danger' | 'info'

const tones: Record<BadgeTone, string> = {
  neutral: 'bg-muted-surface text-muted',
  primary: 'bg-primary-soft text-primary',
  success: 'bg-emerald-500/12 text-emerald-700 dark:text-emerald-400',
  warning: 'bg-amber-500/15 text-amber-700 dark:text-amber-400',
  orange: 'bg-orange-500/14 text-orange-700 dark:text-orange-400',
  danger: 'bg-red-500/12 text-red-700 dark:text-red-400',
  info: 'bg-sky-500/12 text-sky-700 dark:text-sky-400',
}

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: BadgeTone
}

export function Badge({ tone = 'neutral', className, ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-md px-1.5 py-0.5 text-xs font-medium whitespace-nowrap [&_svg]:size-3',
        tones[tone],
        className,
      )}
      {...props}
    />
  )
}
