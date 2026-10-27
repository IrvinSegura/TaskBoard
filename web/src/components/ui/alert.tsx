import { AlertCircle, Info } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils/cn'

interface AlertProps {
  tone?: 'danger' | 'info' | 'warning'
  children: ReactNode
  action?: ReactNode
  className?: string
}

const tones = {
  danger: 'border-danger/30 bg-danger-soft text-danger',
  info: 'border-primary/25 bg-primary-soft text-primary',
  warning: 'border-amber-500/30 bg-amber-500/10 text-amber-700 dark:text-amber-400',
}

export function Alert({ tone = 'danger', children, action, className }: AlertProps) {
  const Icon = tone === 'danger' ? AlertCircle : Info
  return (
    <div
      role={tone === 'danger' ? 'alert' : 'status'}
      className={cn('flex items-start gap-2.5 rounded-lg border px-3 py-2.5 text-sm', tones[tone], className)}
    >
      <Icon className="mt-0.5 size-4 shrink-0" />
      <div className="flex-1">{children}</div>
      {action}
    </div>
  )
}
