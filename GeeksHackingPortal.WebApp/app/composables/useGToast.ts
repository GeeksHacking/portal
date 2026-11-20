/**
 * useGToast: queue short, temporary messages shown by <GToaster> (mounted once in app.vue).
 *
 * Same shape as Nuxt UI's `useToast().add({ title, description, color })`, so migrating is a rename:
 *   color success → ok · error → err · warning → attention · anything else → neutral.
 * Errors stay up longer and are announced assertively; everything else is polite.
 * Toasts pause while hovered or focused, and can be swiped right to dismiss.
 * Don't put the only copy of important information in a toast: it goes away.
 */
export type GToastTone = 'neutral' | 'ok' | 'err' | 'attention'

export interface GToastOptions {
  /** Re-using an id replaces that toast instead of stacking a new one. */
  id?: string
  title: string
  description?: string
  tone?: GToastTone
  /** Nuxt UI colour name, mapped to a tone when `tone` isn't given. */
  color?: string
  /** Defaults to an icon per tone; pass null for none. */
  icon?: string | null
  /** Milliseconds; defaults to 5s (8s for errors). */
  duration?: number
  action?: { label: string, onClick: () => void }
}

export interface GToast extends Omit<GToastOptions, 'color'> {
  id: string
  tone: GToastTone
  open: boolean
}

const MAX_TOASTS = 4
const toasts = ref<GToast[]>([])
let counter = 0

function toneFromColor(color?: string): GToastTone {
  switch (color) {
    case 'success': return 'ok'
    case 'error': return 'err'
    case 'warning': return 'attention'
    default: return 'neutral'
  }
}

export function useGToast() {
  function add(options: GToastOptions) {
    const { color, ...rest } = options
    const id = options.id ?? `g-toast-${++counter}`
    const toast: GToast = { ...rest, id, tone: options.tone ?? toneFromColor(color), open: true }
    const existing = toasts.value.findIndex(t => t.id === id)
    if (existing >= 0) {
      toasts.value.splice(existing, 1, toast)
    }
    else {
      toasts.value.push(toast)
      // oldest go first once the stack is full
      const open = toasts.value.filter(t => t.open)
      open.slice(0, Math.max(0, open.length - MAX_TOASTS)).forEach(t => (t.open = false))
    }
    return id
  }

  /** Starts the close animation; <GToaster> removes it from the queue afterwards. */
  function dismiss(id: string) {
    const toast = toasts.value.find(t => t.id === id)
    if (toast)
      toast.open = false
  }

  function remove(id: string) {
    toasts.value = toasts.value.filter(t => t.id !== id)
  }

  function clear() {
    toasts.value.forEach(t => (t.open = false))
  }

  return {
    toasts: readonly(toasts),
    add,
    dismiss,
    remove,
    clear,
    success: (title: string, options: Omit<GToastOptions, 'title' | 'tone'> = {}) => add({ ...options, title, tone: 'ok' }),
    error: (title: string, options: Omit<GToastOptions, 'title' | 'tone'> = {}) => add({ ...options, title, tone: 'err' }),
  }
}
