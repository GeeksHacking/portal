<script setup lang="ts">
import { DialogContent, DialogDescription, DialogOverlay, DialogPortal, DialogRoot, DialogTitle } from 'reka-ui'
import { G_SHELL_KEY } from '~/composables/useGShell'

/**
 * GShell: the portal frame. A sidebar on the left and the page (<GPanel>) on the right;
 * replaces UDashboardGroup + UDashboardSidebar.
 *
 * - Desktop (≥1024px): the sidebar is always there; it can shrink to an icon rail
 *   (remembered per browser under `storageKey`). The page scrolls inside its panel.
 * - Narrower: the sidebar becomes a drawer, opened by the menu button in <GPanelHeader>.
 *   It traps focus, closes on Esc, the scrim or any navigation, and returns focus on close.
 *
 * Slots (all get `{ collapsed }`; always false in the drawer): `header` (brand), default
 * (navigation, usually <GNavList>), `footer` (account). The page goes in `main`.
 * `label` names the sidebar landmark and the drawer. `contained` fills its parent instead of
 * the window (for previews such as the design lab).
 */
const props = withDefaults(defineProps<{
  label?: string
  storageKey?: string
  contained?: boolean
}>(), {
  label: 'Portal navigation',
  storageKey: 'gh-shell-collapsed',
})

const collapsed = ref(false)
const mobileOpen = ref(false)

onMounted(() => {
  collapsed.value = localStorage.getItem(props.storageKey) === '1'
})
function toggleCollapsed() {
  collapsed.value = !collapsed.value
  localStorage.setItem(props.storageKey, collapsed.value ? '1' : '0')
}
function openMobile() {
  sync()
  mobileOpen.value = true
}

provide(G_SHELL_KEY, { collapsed, mobileOpen, toggleCollapsed, openMobile })

// any link in the drawer closes it, even one to the page you're already on
function closeOnLink(event: MouseEvent) {
  if ((event.target as HTMLElement | null)?.closest('a[href]'))
    mobileOpen.value = false
}

const route = useRoute()
watch(() => route.fullPath, () => {
  mobileOpen.value = false
})

const root = ref<HTMLElement | null>(null)
const { theme, sync } = useGPortalTheme(root)
</script>

<template>
  <div
    ref="root"
    class="g-shell"
    :class="{ 'g-shell--collapsed': collapsed, 'g-shell--contained': contained }"
  >
    <aside
      class="g-shell__sidebar"
      :aria-label="label"
    >
      <div class="g-shell__header">
        <slot
          name="header"
          :collapsed="collapsed"
        />
      </div>
      <div class="g-shell__nav">
        <slot :collapsed="collapsed" />
      </div>
      <div
        v-if="$slots.footer"
        class="g-shell__footer"
      >
        <slot
          name="footer"
          :collapsed="collapsed"
        />
      </div>
    </aside>

    <div class="g-shell__main">
      <slot name="main" />
    </div>

    <DialogRoot v-model:open="mobileOpen">
      <DialogPortal>
        <DialogOverlay
          class="g-shell__scrim"
          :data-gh-theme="theme"
        />
        <DialogContent
          class="g-shell__drawer"
          :data-gh-theme="theme"
          @click="closeOnLink"
        >
          <DialogTitle class="sr-only">
            {{ label }}
          </DialogTitle>
          <DialogDescription class="sr-only">
            Choose a page. Press Escape to close.
          </DialogDescription>
          <div class="g-shell__header">
            <slot
              name="header"
              :collapsed="false"
            />
            <GButton
              variant="ghost"
              size="sm"
              icon="material-symbols:close-rounded"
              aria-label="Close menu"
              class="ml-auto"
              @click="mobileOpen = false"
            />
          </div>
          <div class="g-shell__nav">
            <slot :collapsed="false" />
          </div>
          <div
            v-if="$slots.footer"
            class="g-shell__footer"
          >
            <slot
              name="footer"
              :collapsed="false"
            />
          </div>
        </DialogContent>
      </DialogPortal>
    </DialogRoot>
  </div>
</template>

<!-- not scoped: the drawer is teleported to <body> -->
<style>
.g-shell {
  position: fixed;
  inset: 0;
  display: flex;
  overflow: hidden;
  background: var(--gh-bg);
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.g-shell--contained {
  position: relative;
  inset: auto;
  height: 100%;
}

.g-shell__sidebar {
  display: none;
  flex: none;
  flex-direction: column;
  width: 264px;
  border-right: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-surface);
  transition: width var(--gh-dur-base) var(--ease-gh);
}
.g-shell--collapsed .g-shell__sidebar {
  width: 68px;
}
@media (min-width: 1024px) {
  .g-shell__sidebar { display: flex; }
}
@media (prefers-reduced-motion: reduce) {
  .g-shell__sidebar { transition: none; }
}

.g-shell__header {
  display: flex;
  flex: none;
  align-items: center;
  gap: 8px;
  height: 64px;
  padding: 0 14px;
  border-bottom: var(--gh-border-width) solid var(--gh-line);
}
/* the rail is narrower than the glasses mark at its usual size */
.g-shell--collapsed .g-shell__sidebar .g-shell__header {
  justify-content: center;
  padding: 0;
}
.g-shell--collapsed .g-shell__sidebar .g-shell__header .g-logo {
  --g-logo-height: 20px;
}
.g-shell__nav {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 12px 10px;
}
.g-shell__footer {
  flex: none;
  padding: 10px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}

.g-shell__main {
  display: flex;
  flex: 1;
  min-width: 0;
}
/* whatever the page renders (a <GPanel>, or a wrapper around one) fills the frame */
.g-shell__main > * {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
}

/* mobile drawer */
.g-shell__scrim {
  position: fixed;
  inset: 0;
  z-index: 50;
  background: var(--gh-scrim);
}
.g-shell__drawer {
  position: fixed;
  top: 0;
  bottom: 0;
  left: 0;
  z-index: 51;
  display: flex;
  flex-direction: column;
  width: min(320px, calc(100vw - 40px));
  border-right: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-surface);
  color: var(--gh-text);
  box-shadow: var(--gh-shadow-overlay);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
  outline: none;
}
.g-shell__scrim[data-state='open'] { animation: g-shell-fade var(--gh-dur-base) var(--ease-gh); }
.g-shell__drawer[data-state='open'] { animation: g-shell-in var(--gh-dur-base) var(--ease-gh); }
.g-shell__scrim[data-state='closed'] { animation: g-shell-fade var(--gh-dur-fast) var(--ease-gh) reverse; }
.g-shell__drawer[data-state='closed'] { animation: g-shell-in var(--gh-dur-fast) var(--ease-gh) reverse; }
@keyframes g-shell-fade {
  from { opacity: 0; }
}
@keyframes g-shell-in {
  from { transform: translateX(-100%); }
}
@media (prefers-reduced-motion: reduce) {
  .g-shell__scrim[data-state],
  .g-shell__drawer[data-state] { animation: none; }
}
</style>
