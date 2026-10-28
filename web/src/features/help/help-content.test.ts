import { describe, expect, it } from 'vitest'
import { faqs, searchFaqs, type Faq } from './help-content'

const items: Faq[] = [
  { id: 'a', topic: 'Tableros', question: '¿Cómo archivo un tablero?', answer: 'Desde su menú.' },
  { id: 'b', topic: 'Colaboración', question: '¿Cómo menciono a alguien?', answer: 'Escribe @ en un comentario.' },
]

describe('searchFaqs', () => {
  it('returns everything for an empty query', () => {
    expect(searchFaqs(items, '   ')).toEqual(items)
  })

  it('ignores case and accents', () => {
    expect(searchFaqs(items, 'COLABORACION').map((faq) => faq.id)).toEqual(['b'])
    expect(searchFaqs(items, 'cómo').map((faq) => faq.id)).toEqual(['a', 'b'])
  })

  it('needs every word to match', () => {
    expect(searchFaqs(items, 'archivo menú').map((faq) => faq.id)).toEqual(['a'])
    expect(searchFaqs(items, 'archivo comentario')).toEqual([])
  })
})

describe('faqs', () => {
  it('have unique ids', () => {
    expect(new Set(faqs.map((faq) => faq.id)).size).toBe(faqs.length)
  })
})
