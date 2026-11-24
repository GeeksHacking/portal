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
 * - When an ancestor provides a breadcrumb (provideGPanelCrumb, e.g. the event being managed),
 *   it shows before the title as a link: "GeekHack › Stats". The title stays the heading; the
 *   crumb shortens first when space runs out. On phones it sits on a small line above the title.
 */
withDefaults(defineProps<{
  title?: string
  as?: string
}>(), {
  as: 'h1',
})

const shell = useGShell()
const injectedCrumb = useGPanelCrumb()
const crumb = computed(() => injectedCrumb?.value ?? null)
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
      <div
        v-if="title || $slots.title"
        class="g-panel-header__heading"
        :class="{ 'g-panel-header__heading--crumbed': crumb }"
      >
        <template v-if="crumb">
          <NuxtLink
            :to="crumb.to"
            class="g-panel-header__crumb"
            :title="crumb.label"
          >
            {{ crumb.label }}
          </NuxtLink>
          <Icon
            name="material-symbols:chevron-right-rounded"
            class="g-panel-header__crumb-sep"
            aria-hidden="true"
          />
        </template>
        <component
          :is="as"
          class="g-panel-header__title"
        >
          <slot name="title">
            {{ title }}
          </slot>
        </component>
      </div>
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

/* title, with the breadcrumb before it when there is one */
.g-panel-header__heading {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}
.g-panel-header__heading .g-panel-header__title { min-width: 0; }
.g-panel-header__heading--crumbed .g-panel-header__title { flex: none; }
/* the parent (e.g. the event): muted, and the first thing to shorten */
.g-panel-header__crumb {
  flex: 0 1 auto;
  min-width: 3rem;
  max-width: 24rem;
  overflow: hidden;
  font-size: 15px;
  font-weight: 600;
  color: var(--gh-text-muted);
  text-decoration: none;
  text-overflow: ellipsis;
  white-space: nowrap;
  border-radius: var(--radius-gh-sm);
}
.g-panel-header__crumb:hover {
  color: var(--gh-text);
  text-decoration: underline;
  text-underline-offset: 3px;
}
.g-panel-header__crumb:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.g-panel-header__crumb-sep {
  flex: none;
  width: 18px;
  height: 18px;
  margin: 0 -4px;
  color: var(--gh-text-muted);
}
/* phones: no room for both on one line, so the event goes on a small line above */
@media (max-width: 639px) {
  .g-panel-header__heading--crumbed {
    flex-direction: column;
    align-items: flex-start;
    gap: 0;
  }
  .g-panel-header__heading--crumbed .g-panel-header__title {
    flex: none;
    max-width: 100%;
    font-size: 16px;
  }
  .g-panel-header__crumb {
    max-width: 100%;
    min-width: 0;
    font-size: 12px;
    line-height: 1.3;
  }
  .g-panel-header__crumb-sep { display: none; }
}

.g-panel-header .g-panel-header__collapse { display: none; }
@media (min-width: 1024px) {
  .g-panel-header .g-panel-header__menu { display: none; }
  .g-panel-header .g-panel-header__collapse { display: inline-flex; }
}
</style>
