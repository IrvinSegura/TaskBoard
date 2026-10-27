import type { ActivityEntry } from './activity-api'
import { activityText } from './activity-text'

const entry = (overrides: Partial<ActivityEntry>): ActivityEntry => ({
  id: '1',
  type: 'CardCreated',
  actorId: 'u1',
  actorName: 'Laura',
  boardId: 'b1',
  boardName: 'Roadmap',
  cardId: 'c1',
  subject: null,
  detail: null,
  occurredAt: '2026-10-27T15:00:00Z',
  ...overrides,
})

describe('activityText', () => {
  it('says where a card moved', () => {
    expect(activityText(entry({ type: 'CardMoved', subject: 'Login', detail: 'Hecho' }))).toEqual({
      action: 'movió',
      subject: 'Login',
      detail: 'a Hecho',
    })
  })

  it('puts the assignee in the action', () => {
    expect(activityText(entry({ type: 'CardAssigned', subject: 'Login', detail: 'Diego' })).action).toBe('asignó a Diego a')
  })

  it('works without the names a deleted column left behind', () => {
    expect(activityText(entry({ type: 'ColumnDeleted' }))).toEqual({ action: 'eliminó una columna' })
  })
})
