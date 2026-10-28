// Same list and limits as ChatRules in the API, to reject a file before uploading it.
export const chatExtensions = {
  image: ['.png', '.jpg', '.jpeg', '.gif', '.webp'],
  audio: ['.mp3', '.wav', '.ogg', '.weba', '.m4a'],
  video: ['.mp4', '.webm', '.mov'],
  file: ['.pdf', '.txt', '.md', '.csv', '.zip', '.docx', '.xlsx', '.pptx'],
}

export const allowedChatExtensions = Object.values(chatExtensions).flat()

export const maxChatFileBytes = 25 * 1024 * 1024
export const maxChatMessageBytes = 50 * 1024 * 1024
export const maxChatFiles = 10
export const maxChatBodyLength = 4000

export type AttachmentKind = 'image' | 'audio' | 'video' | 'file'

/** How a stored file is shown: inline image, audio or video player, or a file to download. */
export function attachmentKind(contentType: string): AttachmentKind {
  if (contentType.startsWith('image/')) {
    return 'image'
  }
  if (contentType.startsWith('audio/')) {
    return 'audio'
  }
  if (contentType.startsWith('video/')) {
    return 'video'
  }
  return 'file'
}

function extensionOf(name: string): string {
  return name.includes('.') ? name.slice(name.lastIndexOf('.')).toLowerCase() : ''
}

export function chatFileProblem(file: File): string | null {
  const extension = extensionOf(file.name)
  if (!allowedChatExtensions.includes(extension)) {
    return `No se admiten archivos ${extension || 'sin extensión'}.`
  }
  if (file.size === 0) {
    return 'El archivo está vacío.'
  }
  if (file.size > maxChatFileBytes) {
    return 'El archivo supera el máximo de 25 MB.'
  }
  return null
}

/** Why the files of one message cannot go together, or null when they can. */
export function chatFilesProblem(files: File[]): string | null {
  if (files.length > maxChatFiles) {
    return `Puedes enviar hasta ${maxChatFiles} archivos por mensaje.`
  }
  if (files.reduce((total, file) => total + file.size, 0) > maxChatMessageBytes) {
    return 'Los archivos de un mensaje no pueden pasar de 50 MB en total.'
  }
  return null
}

/**
 * The best format this browser records audio in, with the extension the API expects for it:
 * Chrome and Firefox record WebM (.weba), Safari records MP4 (.m4a).
 */
export function voiceNoteFormat(isSupported: (mimeType: string) => boolean): { mimeType: string; extension: string } | null {
  const candidates = [
    { mimeType: 'audio/webm;codecs=opus', extension: '.weba' },
    { mimeType: 'audio/webm', extension: '.weba' },
    { mimeType: 'audio/mp4', extension: '.m4a' },
    { mimeType: 'audio/ogg;codecs=opus', extension: '.ogg' },
  ]
  return candidates.find((candidate) => isSupported(candidate.mimeType)) ?? null
}
