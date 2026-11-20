<script setup lang="ts">
/**
 * GPanelHeader: the bar at the top of a <GPanel>; replaces UDashboardNavbar +
 * UDashboardSidebarCollapse (pages no longer add the collapse button themselves).
 *
 * - `title` is the page's heading (an <h1> unless `as` says otherwise).
 * - Inside <GShell> it adds the menu button that opens the sidebar drawer (below 1024px) and
 *   the button that shrinks the sidebar to icons (from 1024px).
 * - Slots, in reading order: `leading` (e.g. a back button), `trailing` (right after the
 *   title), `right` (actions at the far end).
 */
withDefaults(defineProps<{
  title?: string
  as?: string
}>(), {
  as: 'h1',
})

const shell = useGShell()
</script>

<template>
  <header class="g-panel-header">
    <div class="g-panel-header__left">
      <template v-if="shell">
        <GButton
          class="g-panel-header__menu"
          variant="ghost"
          size="sm"
          icon="material-symbols:menu-rounded"
          aria-label="Open menu"
          :aria-expanded="shell.mobileOpen.value"
          @click="shell.openMobile()"
        />
        <GButton
          class="g-panel-header__collapse"
          variant="ghost"
          size="sm"
          :icon="shell.collapsed.value ? 'material-symbols:left-panel-open-rounded' : 'material-symbols:left-panel-close-rounded'"
          :aria-label="shell.collapsed.value ? 'Expand sidebar' : 'Collapse sidebar'"
          :aria-pressed="shell.collapsed.value"
          @click="shell.toggleCollapsed()"
        />
      </template>
      <slot name="leading" />
      <component
        :is="as"
        v-if="title || $slots.title"
        class="g-panel-header__title"
      >
        <slot name="title">
          {{ title }}
        </slot>
      </component>
      <slot name="trailing" />
    </div>
    <div
      v-if="$slots.right"
      class="g-panel-header__right"
    >
      <slot name="right" />
    </div>
  </header>
</template>

<style scoped>
.g-panel-header {
  display: flex;
  flex: none;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  height: 64px;
  padding: 0 16px;
  border-bottom: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-bg);
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
@media (min-width: 640px) {
  .g-panel-header { padding: 0 24px; }
}
.g-panel-header__left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}
.g-panel-header__right {
  display: flex;
  flex: none;
  align-items: center;
  gap: 8px;
}
.g-panel-header__title {
  margin: 0;
  overflow: hidden;
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
  line-height: 1.2;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.g-panel-header .g-panel-header__collapse { display: none; }
@media (min-width: 1024px) {
  .g-panel-header .g-panel-header__menu { display: none; }
  .g-panel-header .g-panel-header__collapse { display: inline-flex; }
}
</style>
