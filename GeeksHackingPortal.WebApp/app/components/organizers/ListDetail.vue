<script setup lang="ts">
import { useElementBounding, useMediaQuery, useWindowSize } from '@vueuse/core'

/**
 * List on the left, the picked item's detail on the right (from 1024px), as in a mail app.
 * Below 1024px, or with `dock: false` (e.g. a full-width table view), the detail opens in a
 * dialog instead. The `detail` slot gets `{ close }`; render an OrganizersDetailPane in it.
 * `open` says whether something is picked; closing the panel or dialog sets it to false.
 */
const props = withDefaults(defineProps<{
  /** Names the side panel and the dialog for screen readers. */
  label: string
  dock?: boolean
}>(), {
  dock: true,
})

const open = defineModel<boolean>('open', { default: false })

const isWide = useMediaQuery('(min-width: 1024px)')
const docked = computed(() => props.dock && isWide.value)

const dialogOpen = computed({
  get: () => open.value && !docked.value,
  set: (value) => {
    if (!value)
      open.value = false
  },
})

// the docked panel fits in what's left of the window below its top edge, so its footer
// (the action) is always in view; it grows as the page scrolls and the panel sticks
const detailEl = ref<HTMLElement | null>(null)
const { top: detailTop } = useElementBounding(detailEl)
const { height: windowHeight } = useWindowSize()
const detailMaxHeight = computed(() => `${Math.max(320, windowHeight.value - Math.max(detailTop.value, 0) - 24)}px`)

function close() {
  open.value = false
}

defineExpose({ docked })
</script>

<template>
  <div
    class="old"
    :class="{ 'old--docked': docked }"
  >
    <div class="old__list">
      <slot name="list" />
    </div>
    <aside
      v-if="docked"
      ref="detailEl"
      class="old__detail"
      :style="{ maxHeight: detailMaxHeight }"
      :aria-label="label"
    >
      <slot
        v-if="open"
        name="detail"
        :close="close"
      />
      <div
        v-else
        class="old__placeholder"
      >
        <slot name="placeholder" />
      </div>
    </aside>
  </div>

  <GModal
    v-model:open="dialogOpen"
    :title="label"
    size="lg"
  >
    <template #content>
      <slot
        name="detail"
        :close="close"
      />
    </template>
  </GModal>
</template>

<style scoped>
.old {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}
.old__list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.old--docked {
  display: grid;
  grid-template-columns: minmax(20rem, 2fr) minmax(0, 3fr);
  align-items: start;
  gap: 24px;
}
.old__detail {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  min-height: 20rem;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
  overflow: hidden;
}
.old__placeholder {
  display: flex;
  flex: 1;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
</style>
