<script setup lang="ts">
import { TabsContent, TabsList, TabsRoot, TabsTrigger } from 'reka-ui'

/**
 * GTabs: switch between views (Reka UI Tabs underneath: arrow keys move, Home/End jump).
 *
 * - segmented (default) a compact toggle for 2–4 options, e.g. list/card view. Active = yellow fill.
 * - underline           section tabs across the top of a page or card.
 *
 * Drop-in for the old <UTabs :items :content="false" v-model>. With `content` (default true) each
 * item's panel comes from the slot named after `item.slot ?? item.value`, or the default slot
 * (which receives `{ item }`). With `content: false` only the tab list renders, so it works as a
 * view switcher; give it an `aria-label`.
 */
export interface GTabItem {
  label: string
  value: string
  icon?: string
  disabled?: boolean
  slot?: string
}

defineOptions({ inheritAttrs: false })

withDefaults(defineProps<{
  items: GTabItem[]
  variant?: 'segmented' | 'underline'
  size?: 'sm' | 'md'
  content?: boolean
  block?: boolean
}>(), {
  variant: 'segmented',
  size: 'md',
  content: true,
})

const model = defineModel<string>()
</script>

<template>
  <TabsRoot
    v-model="model"
    class="g-tabs"
    :class="{ 'g-tabs--block': block }"
    :unmount-on-hide="false"
  >
    <TabsList
      v-bind="$attrs"
      class="g-tabs__list"
      :class="[`g-tabs__list--${variant}`, `g-tabs__list--${size}`]"
    >
      <TabsTrigger
        v-for="item in items"
        :key="item.value"
        :value="item.value"
        :disabled="item.disabled"
        class="g-tabs__trigger"
      >
        <Icon
          v-if="item.icon"
          :name="item.icon"
          class="g-tabs__icon"
          aria-hidden="true"
        />
        <span>{{ item.label }}</span>
      </TabsTrigger>
    </TabsList>
    <template v-if="content">
      <TabsContent
        v-for="item in items"
        :key="item.value"
        :value="item.value"
        class="g-tabs__panel"
      >
        <slot
          :name="item.slot ?? item.value"
          :item="item"
        >
          <slot :item="item" />
        </slot>
      </TabsContent>
    </template>
  </TabsRoot>
</template>

<style scoped>
.g-tabs {
  display: flex;
  flex-direction: column;
  gap: 16px;
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}

.g-tabs__list {
  display: flex;
  align-items: stretch;
}
.g-tabs__trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  border: 0;
  background: transparent;
  color: var(--gh-text);
  font: inherit;
  font-weight: 600;
  white-space: nowrap;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.g-tabs__icon { width: 1.1em; height: 1.1em; flex: none; }
.g-tabs__trigger:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
  position: relative;
  z-index: 1;
}
.g-tabs__trigger[data-disabled] {
  color: var(--gh-text-disabled);
  cursor: not-allowed;
  transition: none;
}

/* segmented */
.g-tabs__list--segmented {
  display: inline-flex;
  align-self: flex-start;
  gap: 2px;
  padding: 3px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
}
.g-tabs--block .g-tabs__list--segmented { display: flex; align-self: stretch; }
.g-tabs--block .g-tabs__list--segmented .g-tabs__trigger { flex: 1; }
.g-tabs__list--segmented .g-tabs__trigger {
  border-radius: calc(var(--radius-gh) - 4px);
}
.g-tabs__list--segmented.g-tabs__list--sm .g-tabs__trigger { padding: 4px 10px; font-size: 13px; }
.g-tabs__list--segmented.g-tabs__list--md .g-tabs__trigger { padding: 7px 14px; font-size: 14px; }
.g-tabs__list--segmented .g-tabs__trigger:not([data-state='active'], [data-disabled]):hover {
  background: var(--gh-ghost-hover);
}
.g-tabs__list--segmented .g-tabs__trigger[data-state='active'] {
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}

/* underline */
.g-tabs__list--underline {
  gap: 4px;
  border-bottom: var(--gh-border-width) solid var(--gh-line-soft);
  overflow-x: auto;
}
.g-tabs__list--underline .g-tabs__trigger {
  margin-bottom: calc(-1 * var(--gh-border-width));
  border-bottom: 3px solid transparent;
  color: var(--gh-text-muted);
}
.g-tabs__list--underline.g-tabs__list--sm .g-tabs__trigger { padding: 6px 10px; font-size: 13px; }
.g-tabs__list--underline.g-tabs__list--md .g-tabs__trigger { padding: 10px 14px; font-size: 15px; }
.g-tabs__list--underline .g-tabs__trigger:not([data-state='active'], [data-disabled]):hover {
  color: var(--gh-text);
  background: var(--gh-ghost-hover);
}
.g-tabs__list--underline .g-tabs__trigger[data-state='active'] {
  border-bottom-color: var(--gh-text);
  color: var(--gh-text);
}
/* the list scrolls sideways, which would clip an outside ring: draw it inside the tab */
.g-tabs__list--underline .g-tabs__trigger:focus-visible {
  outline-offset: -3px;
  border-radius: var(--radius-gh-sm) var(--radius-gh-sm) 0 0;
}
.g-tabs__list--underline .g-tabs__trigger[data-disabled] {
  color: var(--gh-text-disabled);
}

.g-tabs__panel:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 4px;
  border-radius: var(--radius-gh-sm);
}
</style>
