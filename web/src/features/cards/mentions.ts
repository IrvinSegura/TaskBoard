// Mentions travel in the comment body as @[Display Name](user:id); the editor shows them as @Display Name.
const tokenPattern = /@\[([^\]\r\n]{1,100})\]\(user:([0-9a-fA-F-]{36})\)/g

export interface Mention {
  id: string
  name: string
}

export type CommentPart = { kind: 'text'; text: string } | { kind: 'mention'; mention: Mention }

/** The body split into plain text and mentions, for rendering. */
export function parseComment(body: string): CommentPart[] {
  const parts: CommentPart[] = []
  let last = 0
  for (const match of body.matchAll(tokenPattern)) {
    if (match.index > last) {
      parts.push({ kind: 'text', text: body.slice(last, match.index) })
    }
    parts.push({ kind: 'mention', mention: { name: match[1]!, id: match[2]! } })
    last = match.index + match[0].length
  }
  if (last < body.length) {
    parts.push({ kind: 'text', text: body.slice(last) })
  }
  return parts
}

/** What the editor shows for a stored body, and the mentions it had. */
export function decodeMentions(body: string): { text: string; mentions: Mention[] } {
  const mentions: Mention[] = []
  const text = body.replace(tokenPattern, (_, name: string, id: string) => {
    if (!mentions.some((m) => m.id === id)) {
      mentions.push({ id, name })
    }
    return `@${name}`
  })
  return { text, mentions }
}

/** Turns every "@Name" the user picked from the list back into a mention token; typed ones stay plain text. */
export function encodeMentions(text: string, mentions: Mention[]): string {
  // Longest names first, so "@Ana María" is not taken by a shorter "@Ana".
  const ordered = [...mentions].sort((a, b) => b.name.length - a.name.length)
  let result = text
  for (const mention of ordered) {
    result = result.split(`@${mention.name}`).join(`@[${mention.name}](user:${mention.id})`)
  }
  return result
}

/** The "@query" being typed right before the caret, if any. */
export function mentionQuery(text: string, caret: number): { start: number; query: string } | null {
  const match = /(^|\s)@([\p{L}\p{N}._-]{0,30})$/u.exec(text.slice(0, caret))
  if (!match) {
    return null
  }
  return { start: caret - match[2]!.length - 1, query: match[2]! }
}

/** Accent- and case-insensitive, so "@sofia" finds "Sofía". */
export function normalize(value: string): string {
  return value.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase()
}
