import { cloneElement, isValidElement, useId, type ReactElement, type ReactNode } from 'react'
import { cn } from '@/lib/utils/cn'

interface FieldProps {
  label: string
  /** The control; it receives id, aria-invalid and aria-describedby. */
  children: ReactElement<Record<string, unknown>>
  error?: string
  hint?: ReactNode
  action?: ReactNode
  className?: string
}

export function Field({ label, children, error, hint, action, className }: FieldProps) {
  const id = useId()
  const messageId = `${id}-message`
  const control = isValidElement(children)
    ? cloneElement(children, {
        id,
        'aria-invalid': error ? true : undefined,
        'aria-describedby': error || hint ? messageId : undefined,
      })
    : children

  return (
    <div className={cn('space-y-1.5', className)}>
      <div className="flex items-center justify-between">
        <label htmlFor={id} className="text-[13px] font-medium text-fg">
          {label}
        </label>
        {action}
      </div>
      {control}
      {error ? (
        <p id={messageId} className="text-[13px] text-danger" role="alert">
          {error}
        </p>
      ) : (
        hint && (
          <p id={messageId} className="text-[13px] text-muted">
            {hint}
          </p>
        )
      )}
    </div>
  )
}
