# 4. Attachment storage

Status: accepted

## Context

Users upload files to cards. Uploaded files are a common attack vector (path traversal, disguised executables,
XSS through files served inline) and must only be readable by board members.

## Decision

- `IFileStorage` abstraction with two implementations: local disk (development) and Azure Blob Storage.
- Allow-list of extensions with a maximum size (`AttachmentRules`). The content type is derived from the extension,
  never taken from the client, and the first bytes are checked against the expected signature.
- Files are stored under a generated key; the original name is kept only in the database.
- Files are never served statically. `GET /attachments/{id}` checks board membership and returns the file as a
  download (`Content-Disposition: attachment`).

## Consequences

- Switching to cloud storage is a configuration change (`FileStorage:Provider`).
- A failed database save after upload leaves an orphaned file; a cleanup job is on the roadmap.
