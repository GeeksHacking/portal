import type { InjectionKey, Ref } from 'vue'

/**
 * The parent a GPanelHeader title sits under, shown as a breadcrumb before it: "GeekHack › Stats".
 * Provided by the layout (e.g. the event being managed), so every page's header picks it up
 * without each page passing it in. `null` shows no breadcrumb.
 */
export interface GPanelCrumb {
  label: string
  to: string
}

const G_PANEL_CRUMB_KEY: InjectionKey<Readonly<Ref<GPanelCrumb | null>>> = Symbol('g-panel-crumb')

export function provideGPanelCrumb(crumb: Readonly<Ref<GPanelCrumb | null>>) {
  provide(G_PANEL_CRUMB_KEY, crumb)
}

export function useGPanelCrumb() {
  return inject(G_PANEL_CRUMB_KEY, null)
}
