import { cn } from '@/lib/utils/cn'
import { labelColorMeta, type BoardLabel, type LabelColor } from './labels-api'

export function LabelChip({ label, className }: { label: BoardLabel; className?: string }) {
  return (
    <span
      className={cn(
        'inline-flex max-w-full items-center truncate rounded-md px-1.5 py-0.5 text-xs font-medium',
        labelColorMeta[label.color].chip,
        className,
      )}
      title={label.name}
    >
      {label.name}
    </span>
  )
}

interface ColorPickerProps {
  value: LabelColor
  onChange: (color: LabelColor) => void
  colors: readonly LabelColor[]
}

export function LabelColorPicker({ value, onChange, colors }: ColorPickerProps) {
  return (
    <div className="flex flex-wrap gap-1.5" role="radiogroup" aria-label="Color">
      {colors.map((color) => (
        <button
          key={color}
          type="button"
          role="radio"
          aria-checked={value === color}
          aria-label={labelColorMeta[color].name}
          title={labelColorMeta[color].name}
          onClick={() => onChange(color)}
          className={cn(
            'size-6 cursor-pointer rounded-md ring-offset-2 ring-offset-surface transition-shadow',
            labelColorMeta[color].swatch,
            value === color ? 'ring-2 ring-primary' : 'hover:ring-2 hover:ring-line-strong',
          )}
        />
      ))}
    </div>
  )
}
