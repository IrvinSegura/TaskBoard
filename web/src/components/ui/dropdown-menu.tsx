import * as RadixMenu from '@radix-ui/react-dropdown-menu'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils/cn'

interface DropdownMenuProps {
  trigger: ReactNode
  children: ReactNode
  align?: 'start' | 'center' | 'end'
  className?: string
  open?: boolean
  onOpenChange?: (open: boolean) => void
}

export function DropdownMenu({ trigger, children, align = 'end', className, open, onOpenChange }: DropdownMenuProps) {
  return (
    <RadixMenu.Root open={open} onOpenChange={onOpenChange}>
      <RadixMenu.Trigger asChild>{trigger}</RadixMenu.Trigger>
      <RadixMenu.Portal>
        <RadixMenu.Content
          align={align}
          sideOffset={6}
          className={cn(
            'z-50 min-w-48 rounded-xl border border-line bg-surface p-1 shadow-overlay data-[state=open]:animate-fade-in',
            className,
          )}
        >
          {children}
        </RadixMenu.Content>
      </RadixMenu.Portal>
    </RadixMenu.Root>
  )
}

interface MenuItemProps {
  children: ReactNode
  onSelect?: () => void
  icon?: ReactNode
  destructive?: boolean
  disabled?: boolean
}

export function MenuItem({ children, onSelect, icon, destructive, disabled }: MenuItemProps) {
  return (
    <RadixMenu.Item
      disabled={disabled}
      onSelect={onSelect}
      className={cn(
        'flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm outline-none select-none [&_svg]:size-4',
        'data-[disabled]:pointer-events-none data-[disabled]:opacity-50',
        destructive
          ? 'text-danger data-[highlighted]:bg-danger-soft'
          : 'text-fg data-[highlighted]:bg-muted-surface [&_svg]:text-muted',
      )}
    >
      {icon}
      {children}
    </RadixMenu.Item>
  )
}

export function MenuSeparator() {
  return <RadixMenu.Separator className="my-1 h-px bg-line" />
}

export function MenuLabel({ children }: { children: ReactNode }) {
  return <RadixMenu.Label className="px-2.5 pt-2 pb-1 text-xs font-medium text-faint">{children}</RadixMenu.Label>
}
