import { useCallback, useEffect, useRef, useState } from 'react'
import { voiceNoteFormat } from './chat-files'

export function canRecordVoice(): boolean {
  return typeof MediaRecorder !== 'undefined' && !!navigator.mediaDevices?.getUserMedia
}

/** Records a voice note from the microphone. stop() resolves with the file, ready to send like any other. */
export function useVoiceRecorder() {
  const [recording, setRecording] = useState(false)
  const [seconds, setSeconds] = useState(0)
  const recorder = useRef<MediaRecorder | null>(null)
  const chunks = useRef<Blob[]>([])
  const extension = useRef('.weba')
  const timer = useRef<ReturnType<typeof setInterval> | undefined>(undefined)

  const release = useCallback(() => {
    clearInterval(timer.current)
    recorder.current?.stream.getTracks().forEach((track) => track.stop())
    recorder.current = null
    chunks.current = []
    setRecording(false)
    setSeconds(0)
  }, [])

  // Leaving the page while recording turns the microphone off.
  useEffect(() => release, [release])

  const start = async () => {
    const format = voiceNoteFormat((type) => MediaRecorder.isTypeSupported(type))
    if (!format) {
      throw new Error('Este navegador no puede grabar audio.')
    }
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
    const next = new MediaRecorder(stream, { mimeType: format.mimeType })
    chunks.current = []
    next.ondataavailable = (event) => event.data.size > 0 && chunks.current.push(event.data)
    next.start(250)
    recorder.current = next
    extension.current = format.extension
    setRecording(true)
    setSeconds(0)
    const startedAt = Date.now()
    timer.current = setInterval(() => setSeconds(Math.floor((Date.now() - startedAt) / 1000)), 250)
  }

  const stop = () =>
    new Promise<File | null>((resolve) => {
      const current = recorder.current
      if (!current) {
        resolve(null)
        return
      }
      current.onstop = () => {
        const type = current.mimeType.split(';')[0] || 'audio/webm'
        const blob = new Blob(chunks.current, { type })
        const stamp = new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')
        release()
        resolve(blob.size > 0 ? new File([blob], `nota-de-voz-${stamp}${extension.current}`, { type }) : null)
      }
      current.stop()
    })

  const cancel = () => {
    if (recorder.current) {
      recorder.current.onstop = null
      recorder.current.stop()
    }
    release()
  }

  return { recording, seconds, start, stop, cancel }
}
