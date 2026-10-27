import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

export function SectionTitle({ icon: Icon, children, action }: { icon: LucideIcon; children: ReactNode; action?: ReactNode }) {
  return (
    <div className="mb-3 flex items-center gap-2">
      <Icon className="size-4 text-muted" />
      <h3 className="text-sm font-semibold">{children}</h3>
      {action && <div className="ml-auto">{action}</div>}
    </div>
  )
}
