import { useEffect, useState } from 'react'
import { chatApi } from './chat-api'

interface AttachmentUrl {
  url: string | null
  error: unknown
}

/**
 * Downloads a chat file with the bearer token and gives back an object URL for <img>, <audio> or <video>.
 * The URL is revoked when the component unmounts. Nothing is fetched until enabled is true.
 */
export function useAttachmentUrl(attachmentId: string, enabled = true): AttachmentUrl {
  const [state, setState] = useState<AttachmentUrl & { id: string | null }>({ id: null, url: null, error: null })

  useEffect(() => {
    if (!enabled) {
      return
    }
    let url: string | null = null
    let cancelled = false
    chatApi
      .attachmentBlob(attachmentId)
      .then((blob) => {
        url = URL.createObjectURL(blob)
        if (cancelled) {
          URL.revokeObjectURL(url)
        } else {
          setState({ id: attachmentId, url, error: null })
        }
      })
      .catch((error: unknown) => !cancelled && setState({ id: attachmentId, url: null, error }))
    return () => {
      cancelled = true
      if (url) {
        URL.revokeObjectURL(url)
      }
    }
  }, [attachmentId, enabled])

  // A result for another file (the id changed) is not this one's.
  return state.id === attachmentId ? state : { url: null, error: null }
}

/** Saves a chat file under its original name. */
export async function downloadAttachment(attachmentId: string, fileName: string) {
  const url = URL.createObjectURL(await chatApi.attachmentBlob(attachmentId))
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.append(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 10_000)
}
