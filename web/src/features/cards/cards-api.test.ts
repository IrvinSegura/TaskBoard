import { attachmentProblem } from './cards-api'

function file(name: string, size: number): File {
  const content = new Uint8Array(size)
  return new File([content], name)
}

describe('attachmentProblem', () => {
  it('accepts the formats the API allows, in any case', () => {
    expect(attachmentProblem(file('report.PDF', 1024))).toBeNull()
    expect(attachmentProblem(file('photo.jpeg', 2048))).toBeNull()
  })

  it('rejects other extensions and files without one', () => {
    expect(attachmentProblem(file('script.exe', 10))).toMatch(/\.exe/)
    expect(attachmentProblem(file('README', 10))).toMatch(/sin extensión/)
  })

  it('rejects empty files and files over 10 MB', () => {
    expect(attachmentProblem(file('empty.txt', 0))).toMatch(/vacío/)
    expect(attachmentProblem(file('big.zip', 10 * 1024 * 1024 + 1))).toMatch(/10 MB/)
  })
})
