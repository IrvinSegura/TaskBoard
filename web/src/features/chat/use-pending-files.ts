import { useState } from 'react'
import { toast } from 'sonner'
import { chatFileProblem, maxChatFiles } from './chat-files'

/** The files waiting to go with the next message. Owned by the thread, so files dropped anywhere on it land here. */
export function usePendingFiles() {
  const [files, setFiles] = useState<File[]>([])

  const add = (incoming: File[]) => {
    const accepted: File[] = []
    for (const file of incoming) {
      const problem = chatFileProblem(file)
      if (problem) {
        toast.error(`${file.name}: ${problem}`)
      } else {
        accepted.push(file)
      }
    }
    const room = maxChatFiles - files.length
    if (accepted.length > room) {
      toast.error(`Puedes enviar hasta ${maxChatFiles} archivos por mensaje.`)
    }
    if (accepted.length > 0 && room > 0) {
      setFiles([...files, ...accepted.slice(0, room)])
    }
  }

  return { files, add, set: setFiles }
}

export type PendingFiles = ReturnType<typeof usePendingFiles>
