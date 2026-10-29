import type { ExportStat, ExportTone } from './export-document'

// The app's light theme in RGB, shared by the PDF and the workbook so both files look alike.
export type Rgb = [number, number, number]

export const colors = {
  brand: [79, 70, 229] as Rgb,
  brandDark: [55, 48, 163] as Rgb,
  brandSoft: [238, 242, 255] as Rgb,
  brandTint: [199, 210, 254] as Rgb,
  ink: [17, 24, 39] as Rgb,
  body: [55, 65, 81] as Rgb,
  muted: [107, 114, 128] as Rgb,
  faint: [156, 163, 175] as Rgb,
  line: [229, 231, 235] as Rgb,
  stripe: [249, 250, 252] as Rgb,
  card: [248, 248, 252] as Rgb,
  white: [255, 255, 255] as Rgb,
}

export const toneColors: Record<ExportTone, Rgb> = {
  danger: [220, 38, 38],
  warning: [217, 119, 6],
  success: [5, 150, 105],
  muted: colors.faint,
}

export const statColors: Record<NonNullable<ExportStat['tone']>, Rgb> = {
  primary: colors.brand,
  success: toneColors.success,
  danger: toneColors.danger,
  warning: toneColors.warning,
}

/** "FF4F46E5", the ARGB form Excel wants. */
export function argb([r, g, b]: Rgb): string {
  return `FF${[r, g, b].map((part) => part.toString(16).padStart(2, '0')).join('').toUpperCase()}`
}
