import * as RadixDialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils/cn'

interface DialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: ReactNode
  description?: ReactNode
  children: ReactNode
  footer?: ReactNode
  size?: 'sm' | 'md' | 'lg' | 'xl'
  /** Hides the title visually while keeping it for screen readers. */
  hideHeader?: boolean
  className?: string
}

const widths = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-xl',
  xl: 'max-w-4xl',
}

export function Dialog({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
  size = 'md',
  hideHeader = false,
  className,
}: DialogProps) {
  return (
    <RadixDialog.Root open={open} onOpenChange={onOpenChange}>
      <RadixDialog.Portal>
        <RadixDialog.Overlay className="fixed inset-0 z-50 bg-black/40 backdrop-blur-[2px] data-[state=open]:animate-fade-in" />
        <RadixDialog.Content
          className={cn(
            'fixed top-1/2 left-1/2 z-50 flex max-h-[calc(100dvh-2rem)] w-[calc(100vw-2rem)] -translate-x-1/2 -translate-y-1/2 flex-col',
            'rounded-2xl border border-line bg-surface shadow-overlay focus:outline-none data-[state=open]:animate-scale-in',
            widths[size],
            className,
          )}
        >
          {hideHeader ? (
            <RadixDialog.Title className="sr-only">{title}</RadixDialog.Title>
          ) : (
            <div className="flex items-start justify-between gap-4 px-6 pt-5 pb-1">
              <div>
                <RadixDialog.Title className="text-[17px] font-semibold tracking-tight">{title}</RadixDialog.Title>
                {description && (
                  <RadixDialog.Description className="mt-1 text-sm text-muted">{description}</RadixDialog.Description>
                )}
              </div>
              <RadixDialog.Close
                className="-mr-2 rounded-md p-1.5 text-faint transition-colors hover:bg-muted-surface hover:text-fg"
                aria-label="Cerrar"
              >
                <X className="size-4" />
              </RadixDialog.Close>
            </div>
          )}
          {!description && <RadixDialog.Description className="sr-only">{title}</RadixDialog.Description>}
          <div className={cn('scrollbar-thin overflow-y-auto', !hideHeader && 'px-6 py-4')}>{children}</div>
          {footer && (
            <div className="flex flex-col-reverse gap-2 border-t border-line px-6 py-4 sm:flex-row sm:justify-end">
              {footer}
            </div>
          )}
        </RadixDialog.Content>
      </RadixDialog.Portal>
    </RadixDialog.Root>
  )
}
