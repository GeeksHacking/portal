import type { Ref } from 'vue'

/**
 * Content teleported to <body> (select menus, modals) leaves the element that set
 * `data-gh-theme`, so it would fall back to the page theme. Call `sync()` when the
 * overlay opens: `theme` then holds the nearest `data-gh-theme` above `anchor`, to be
 * bound back onto the teleported root. When the theme comes from `.dark` on <html>
 * (Nuxt color-mode) nothing is needed, so `theme` stays undefined.
 */
export function useGPortalTheme(anchor: Ref<HTMLElement | null | undefined>) {
  const theme = ref<string | undefined>()
  function sync() {
    theme.value = anchor.value?.closest('[data-gh-theme]')?.getAttribute('data-gh-theme') ?? undefined
  }
  return { theme, sync }
}
