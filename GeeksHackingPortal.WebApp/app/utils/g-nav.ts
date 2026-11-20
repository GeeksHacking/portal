/** Navigation data for <GNavList>. */
export interface GNavItem {
  label: string
  icon?: string
  to?: string
  /** only the exact path counts as current (not its sub-pages) */
  exact?: boolean
  /** one level of sub-pages, shown as a disclosure */
  children?: GNavItem[]
}

export interface GNavGroup {
  label?: string
  items: GNavItem[]
}
