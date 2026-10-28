import { parseComment } from './mentions'

/** A comment's text with its mentions as highlighted names. */
export function CommentBody({ body }: { body: string }) {
  return (
    <>
      {parseComment(body).map((part, i) =>
        part.kind === 'text' ? (
          <span key={i}>{part.text}</span>
        ) : (
          <span key={i} className="rounded bg-primary-soft px-1 font-medium text-primary">
            @{part.mention.name}
          </span>
        ),
      )}
    </>
  )
}
