<script setup lang="ts">
/**
 * GPanel: one page inside <GShell>; replaces UDashboardPanel.
 *
 * `header` slot (usually <GPanelHeader>) stays at the top; `body` (or the default slot) scrolls
 * underneath, padded and laid out as a column with the same gaps UDashboardPanel used, so pages
 * swap over without layout changes.
 */
defineProps<{
  id?: string
}>()
</script>

<template>
  <section
    :id="id"
    class="g-panel"
  >
    <slot name="header" />
    <div class="g-panel__body">
      <slot name="body">
        <slot />
      </slot>
    </div>
  </section>
</template>

<style scoped>
.g-panel {
  position: relative;
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
  min-height: 100svh;
}
/* inside the shell the panel fills the fixed frame and its body is what scrolls */
:where(.g-shell__main) .g-panel {
  min-height: 0;
}
.g-panel__body {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 16px;
  min-height: 0;
  overflow-y: auto;
  padding: 16px;
}
@media (min-width: 640px) {
  .g-panel__body { gap: 24px; padding: 24px; }
}
</style>
