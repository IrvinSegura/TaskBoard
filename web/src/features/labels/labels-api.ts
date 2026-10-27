import { api, type CreatedResponse } from '@/lib/api/client'

// Same order as the API's LabelColor enum.
export const labelColors = ['Gray', 'Red', 'Orange', 'Yellow', 'Green', 'Teal', 'Blue', 'Purple', 'Pink'] as const
export type LabelColor = (typeof labelColors)[number]

export interface BoardLabel {
  id: string
  name: string
  color: LabelColor
}

/** Chip and swatch classes per color, written out in full so Tailwind finds them. */
export const labelColorMeta: Record<LabelColor, { name: string; chip: string; swatch: string }> = {
  Gray: { name: 'Gris', chip: 'bg-slate-500/15 text-slate-700 dark:text-slate-300', swatch: 'bg-slate-500' },
  Red: { name: 'Rojo', chip: 'bg-red-500/15 text-red-700 dark:text-red-300', swatch: 'bg-red-500' },
  Orange: { name: 'Naranja', chip: 'bg-orange-500/15 text-orange-700 dark:text-orange-300', swatch: 'bg-orange-500' },
  Yellow: { name: 'Amarillo', chip: 'bg-yellow-400/25 text-yellow-800 dark:text-yellow-300', swatch: 'bg-yellow-400' },
  Green: { name: 'Verde', chip: 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300', swatch: 'bg-emerald-500' },
  Teal: { name: 'Turquesa', chip: 'bg-teal-500/15 text-teal-700 dark:text-teal-300', swatch: 'bg-teal-500' },
  Blue: { name: 'Azul', chip: 'bg-blue-500/15 text-blue-700 dark:text-blue-300', swatch: 'bg-blue-500' },
  Purple: { name: 'Morado', chip: 'bg-violet-500/15 text-violet-700 dark:text-violet-300', swatch: 'bg-violet-500' },
  Pink: { name: 'Rosa', chip: 'bg-pink-500/15 text-pink-700 dark:text-pink-300', swatch: 'bg-pink-500' },
}

/** The labels with these ids, in the board's order; ids of deleted labels are skipped. */
export function labelsByIds(labels: BoardLabel[], ids: string[]): BoardLabel[] {
  const wanted = new Set(ids)
  return labels.filter((label) => wanted.has(label.id))
}

export const labelsApi = {
  create: (boardId: string, name: string, color: LabelColor) =>
    api.post<CreatedResponse>(`/boards/${boardId}/labels`, { name, color }),
  update: (boardId: string, labelId: string, name: string, color: LabelColor) =>
    api.put(`/boards/${boardId}/labels/${labelId}`, { name, color }),
  delete: (boardId: string, labelId: string) => api.delete(`/boards/${boardId}/labels/${labelId}`),
  addToCard: (cardId: string, labelId: string) => api.post(`/cards/${cardId}/labels`, { labelId }),
  removeFromCard: (cardId: string, labelId: string) => api.delete(`/cards/${cardId}/labels/${labelId}`),
}
