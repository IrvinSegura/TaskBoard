export type TextSegment = { type: 'text'; text: string } | { type: 'link'; text: string; href: string }

// http(s) links and bare "www." ones. Trailing punctuation belongs to the sentence, not the link.
const linkPattern = /\b(?:https?:\/\/|www\.)[^\s<>"]+[^\s<>".,:;!?)\]}'’]/gi

/** Splits a message into plain text and links, so links render as anchors without any HTML from the text. */
export function linkify(text: string): TextSegment[] {
  const segments: TextSegment[] = []
  let last = 0
  for (const match of text.matchAll(linkPattern)) {
    const start = match.index
    if (start > last) {
      segments.push({ type: 'text', text: text.slice(last, start) })
    }
    const url = match[0]
    segments.push({ type: 'link', text: url, href: url.toLowerCase().startsWith('www.') ? `https://${url}` : url })
    last = start + url.length
  }
  if (last < text.length) {
    segments.push({ type: 'text', text: text.slice(last) })
  }
  return segments
}
