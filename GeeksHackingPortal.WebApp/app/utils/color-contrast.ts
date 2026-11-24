// WCAG 2.x contrast helpers, used by /design-lab to check tokens live.

export interface Rgba { r: number, g: number, b: number, a: number }

export function parseCssColor(value: string): Rgba | null {
  const match = value.match(/rgba?\(([^)]+)\)/)
  if (!match?.[1])
    return null
  const parts = match[1].split(/[\s,/]+/).filter(Boolean).map(Number)
  const [r = 0, g = 0, b = 0, a = 1] = parts
  return { r, g, b, a }
}

function channel(value: number) {
  const c = value / 255
  return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
}

export function relativeLuminance({ r, g, b }: Rgba) {
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b)
}

/** Composite a (possibly translucent) colour over an opaque one. */
export function flatten(top: Rgba, under: Rgba): Rgba {
  const a = top.a
  return {
    r: top.r * a + under.r * (1 - a),
    g: top.g * a + under.g * (1 - a),
    b: top.b * a + under.b * (1 - a),
    a: 1,
  }
}

export function contrastRatio(foreground: Rgba, background: Rgba) {
  const fg = flatten(foreground, background)
  const l1 = relativeLuminance(fg)
  const l2 = relativeLuminance(background)
  return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05)
}

export function toHex({ r, g, b }: Rgba) {
  return `#${[r, g, b].map(v => Math.round(v).toString(16).padStart(2, '0')).join('').toUpperCase()}`
}
