import { notificationText } from './notification-text'

describe('notificationText', () => {
  it('translates the messages the API sends', () => {
    expect(notificationText('You were assigned to "Login screen".')).toBe('Te asignaron la tarjeta «Login screen».')
    expect(notificationText('You were added to the board "Roadmap".')).toBe('Te agregaron al tablero «Roadmap».')
    expect(notificationText('You were added to the team "Producto".')).toBe('Te agregaron al equipo «Producto».')
  })

  it('translates mentions and comments with the person who wrote them', () => {
    expect(notificationText('Carlos Ruiz mentioned you in "Push".')).toBe('Carlos Ruiz te mencionó en «Push».')
    expect(notificationText('Diego commented on "Push".')).toBe('Diego comentó en «Push».')
  })

  it('keeps names with quotes inside', () => {
    expect(notificationText('You were assigned to "Fix "quotes" bug".')).toBe('Te asignaron la tarjeta «Fix "quotes" bug».')
  })

  it('leaves unknown messages as they are', () => {
    expect(notificationText('Something new')).toBe('Something new')
  })
})
