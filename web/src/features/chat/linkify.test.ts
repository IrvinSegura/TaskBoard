import { describe, expect, it } from 'vitest'
import { linkify } from './linkify'

describe('linkify', () => {
  it('keeps plain text as one segment', () => {
    expect(linkify('Hola equipo')).toEqual([{ type: 'text', text: 'Hola equipo' }])
  })

  it('finds http and https links in the middle of a sentence', () => {
    expect(linkify('Mira https://example.com/plan?id=4 y http://a.io')).toEqual([
      { type: 'text', text: 'Mira ' },
      { type: 'link', text: 'https://example.com/plan?id=4', href: 'https://example.com/plan?id=4' },
      { type: 'text', text: ' y ' },
      { type: 'link', text: 'http://a.io', href: 'http://a.io' },
    ])
  })

  it('adds https to www links', () => {
    expect(linkify('www.example.com')).toEqual([
      { type: 'link', text: 'www.example.com', href: 'https://www.example.com' },
    ])
  })

  it('leaves trailing punctuation out of the link', () => {
    expect(linkify('¿Viste https://example.com/doc?')).toEqual([
      { type: 'text', text: '¿Viste ' },
      { type: 'link', text: 'https://example.com/doc', href: 'https://example.com/doc' },
      { type: 'text', text: '?' },
    ])
    expect(linkify('(https://example.com).')[1]).toEqual({
      type: 'link',
      text: 'https://example.com',
      href: 'https://example.com',
    })
  })

  it('never turns other schemes into links', () => {
    expect(linkify('javascript:alert(1)')).toEqual([{ type: 'text', text: 'javascript:alert(1)' }])
  })
})
