import { decodeMentions, encodeMentions, mentionQuery, normalize, parseComment } from './mentions'

const laura = { id: '01a17ea5-0000-7000-8000-000000000001', name: 'Laura Méndez' }
const ana = { id: '01a17ea5-0000-7000-8000-000000000002', name: 'Ana' }
const anaMaria = { id: '01a17ea5-0000-7000-8000-000000000003', name: 'Ana María' }

describe('mentions', () => {
  it('encodes picked names and decodes them back', () => {
    const body = encodeMentions('Hola @Laura Méndez, mira esto', [laura])
    expect(body).toBe(`Hola @[Laura Méndez](user:${laura.id}), mira esto`)
    expect(decodeMentions(body)).toEqual({ text: 'Hola @Laura Méndez, mira esto', mentions: [laura] })
  })

  it('prefers the longest name', () => {
    expect(encodeMentions('@Ana María y @Ana', [ana, anaMaria])).toBe(
      `@[Ana María](user:${anaMaria.id}) y @[Ana](user:${ana.id})`,
    )
  })

  it('splits a body into text and mentions', () => {
    expect(parseComment(`Hola @[Laura Méndez](user:${laura.id})!`)).toEqual([
      { kind: 'text', text: 'Hola ' },
      { kind: 'mention', mention: laura },
      { kind: 'text', text: '!' },
    ])
  })

  it('finds the query being typed, but not inside emails', () => {
    expect(mentionQuery('Hola @lau', 9)).toEqual({ start: 5, query: 'lau' })
    expect(mentionQuery('@', 1)).toEqual({ start: 0, query: '' })
    expect(mentionQuery('soporte@empresa', 15)).toBeNull()
  })

  it('compares names without accents or case', () => {
    expect(normalize('Sofía')).toBe(normalize('SOFIA'))
  })
})
