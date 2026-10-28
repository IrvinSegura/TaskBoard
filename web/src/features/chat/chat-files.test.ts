import { describe, expect, it } from 'vitest'
import { attachmentKind, chatFileProblem, chatFilesProblem, maxChatFileBytes, voiceNoteFormat } from './chat-files'

function fileOf(name: string, size: number): File {
  const file = new File(['x'], name)
  Object.defineProperty(file, 'size', { value: size })
  return file
}

describe('chat files', () => {
  it('shows images, audio and video inline and everything else as a file', () => {
    expect(attachmentKind('image/webp')).toBe('image')
    expect(attachmentKind('audio/webm')).toBe('audio')
    expect(attachmentKind('video/mp4')).toBe('video')
    expect(attachmentKind('application/pdf')).toBe('file')
  })

  it('rejects types the API does not accept, empty files and files over 25 MB', () => {
    expect(chatFileProblem(fileOf('foto.PNG', 10))).toBeNull()
    expect(chatFileProblem(fileOf('nota.weba', 10))).toBeNull()
    expect(chatFileProblem(fileOf('setup.exe', 10))).toMatch('.exe')
    expect(chatFileProblem(fileOf('vacio.txt', 0))).toMatch('vacío')
    expect(chatFileProblem(fileOf('video.mp4', maxChatFileBytes + 1))).toMatch('25 MB')
  })

  it('limits the files of one message by count and total size', () => {
    expect(chatFilesProblem([fileOf('a.png', 10)])).toBeNull()
    expect(chatFilesProblem(Array.from({ length: 11 }, () => fileOf('a.png', 10)))).toMatch('10 archivos')
    expect(chatFilesProblem([1, 2, 3].map(() => fileOf('a.mp4', maxChatFileBytes)))).toMatch('50 MB')
  })

  it('records voice notes in the first format the browser supports', () => {
    expect(voiceNoteFormat((type) => type.startsWith('audio/webm'))).toEqual({
      mimeType: 'audio/webm;codecs=opus',
      extension: '.weba',
    })
    expect(voiceNoteFormat((type) => type === 'audio/mp4')?.extension).toBe('.m4a')
    expect(voiceNoteFormat(() => false)).toBeNull()
  })
})
