/**
 * Markdown → plain text, for places that show a description as text rather than rendering it:
 * event cards (clamped by CSS), search, and calendar invites.
 *
 * Not a full Markdown parser: it removes the syntax organisers can type with the description
 * editor (headings, emphasis, lists, quotes, links, code, rules) and any raw HTML tags.
 *
 * - default: one line, paragraphs joined by spaces (for cards and search)
 * - `{ keepLines: true }`: one line per paragraph or list item (for calendar invites)
 */
export function markdownToPlainText(markdown: string | null | undefined, options: { keepLines?: boolean } = {}): string {
  if (!markdown)
    return ''

  const lines = markdown
    .replace(/\r\n?/g, '\n')
    // fenced code: keep the code, drop the fences
    .replace(/^\s*(```|~~~).*$/gm, '')
    .split('\n')
    .map(line => line
      // block syntax at the start of a line
      .replace(/^\s{0,3}#{1,6}\s+/, '') // # heading
      .replace(/^\s{0,3}(?:[-*_]\s*){3,}$/, '') // --- rule
      .replace(/^(\s*>\s?)+/, '') // > quote
      .replace(/^\s*(?:[-*+]|\d{1,9}[.)])\s+(?:\[[ x]\]\s+)?/i, '') // list item (and task box)
      // inline syntax
      .replace(/<[^>]*>/g, '') // raw HTML tags
      .replace(/!\[([^\]]*)\]\((?:[^()]|\([^()]*\))*\)/g, '$1') // ![alt](src)
      .replace(/\[([^\]]+)\]\((?:[^()]|\([^()]*\))*\)/g, '$1') // [text](url), one level of () in the url
      .replace(/(`+)(.+?)\1/g, '$2') // `code`
      .replace(/(\*\*|__)(.+?)\1/g, '$2') // **bold**
      .replace(/(\*|_)(.+?)\1/g, '$2') // *italic*
      .replace(/~~(.+?)~~/g, '$1') // ~~strike~~
      .replace(/\\([\\`*_{}[\]()#+\-.!>~|])/g, '$1') // backslash escapes
      .replace(/\s+$/, '')
      .trim())
    .filter(Boolean)

  return options.keepLines ? lines.join('\n') : lines.join(' ')
}
