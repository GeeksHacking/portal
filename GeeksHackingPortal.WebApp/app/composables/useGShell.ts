import type { InjectionKey, Ref } from 'vue'

/**
 * Shared state of the nearest <GShell>: the desktop sidebar's icon-rail mode and the mobile
 * drawer. <GPanelHeader> uses it for its menu / collapse buttons. Outside a shell it returns
 * null, so headers rendered elsewhere (e.g. the design lab) just skip those buttons.
 */
export interface GShellState {
  collapsed: Ref<boolean>
  mobileOpen: Ref<boolean>
  toggleCollapsed: () => void
  openMobile: () => void
}

export const G_SHELL_KEY: InjectionKey<GShellState> = Symbol('g-shell')

export function useGShell() {
  return inject(G_SHELL_KEY, null)
}
