<script setup lang="ts">
import {
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogOverlay,
  DialogPortal,
  DialogRoot,
  DialogTitle,
} from 'reka-ui'

/**
 * GModal: dialog with focus trap, scroll lock and focus return (Reka UI Dialog underneath).
 *
 * Drop-in for the old <UModal v-model:open>:
 * - Default layout: `title` (+ optional `description`) header with a close button, then the
 *   `body` slot (or default slot), then the `footer` slot for actions.
 * - `content` slot replaces the whole inside (the pattern most old modals use). `title` is
 *   still required for screen readers; with the `content` slot it is announced but not shown.
 *   The slot gets `{ close }`.
 * - `dismissible: false` turns off Esc, clicking outside and the close button, for flows that
 *   must be answered (e.g. confirm-before-leaving).
 *
 * Classes and other attributes land on the dialog panel.
 */
defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  title: string
  description?: string
  size?: 'sm' | 'md' | 'lg'
  dismissible?: boolean
}>(), {
  size: 'md',
  dismissible: true,
})

const emit = defineEmits<{
  afterLeave: []
}>()

const open = defineModel<boolean>('open', { default: false })

const slots = useSlots()
const customContent = computed(() => !!slots.content)

const anchor = ref<HTMLElement | null>(null)
const { theme, sync } = useGPortalTheme(anchor)
watch(open, (isOpen) => {
  if (isOpen)
    sync()
}, { immediate: true })

function close() {
  open.value = false
}

function guard(event: Event) {
  if (!props.dismissible)
    event.preventDefault()
}
</script>

<template>
  <span
    ref="anchor"
    hidden
  />
  <DialogRoot v-model:open="open">
    <DialogPortal>
      <DialogOverlay
        class="g-modal__overlay"
        :data-gh-theme="theme"
      />
      <DialogContent
        v-bind="$attrs"
        class="g-modal"
        :class="`g-modal--${size}`"
        :data-gh-theme="theme"
        @escape-key-down="guard"
        @pointer-down-outside="guard"
        @interact-outside="guard"
        @close-auto-focus="emit('afterLeave')"
      >
        <template v-if="customContent">
          <DialogTitle class="sr-only">
            {{ title }}
          </DialogTitle>
          <DialogDescription
            v-if="description"
            class="sr-only"
          >
            {{ description }}
          </DialogDescription>
          <slot
            name="content"
            :close="close"
          />
        </template>
        <template v-else>
          <header class="g-modal__header">
            <div class="g-modal__heading">
              <slot name="header">
                <DialogTitle class="g-modal__title">
                  {{ title }}
                </DialogTitle>
                <DialogDescription
                  v-if="description"
                  class="g-modal__description"
                >
                  {{ description }}
                </DialogDescription>
              </slot>
            </div>
            <DialogClose
              v-if="dismissible"
              as-child
            >
              <GButton
                variant="ghost"
                size="sm"
                icon="i-lucide-x"
                aria-label="Close"
              />
            </DialogClose>
          </header>
          <div class="g-modal__body">
            <slot name="body">
              <slot />
            </slot>
          </div>
          <footer
            v-if="$slots.footer"
            class="g-modal__footer"
          >
            <slot
              name="footer"
              :close="close"
            />
          </footer>
        </template>
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>

<!-- not scoped: the dialog is teleported to <body> -->
<style>
.g-modal__overlay {
  position: fixed;
  inset: 0;
  z-index: 50;
  background: var(--gh-scrim);
}

.g-modal {
  position: fixed;
  left: 50%;
  top: 50%;
  z-index: 51;
  display: flex;
  flex-direction: column;
  width: calc(100vw - 32px);
  max-height: calc(100dvh - 32px);
  transform: translate(-50%, -50%);
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface-raised);
  color: var(--gh-text);
  box-shadow: var(--gh-shadow-overlay);
  font-family: var(--font-body);
  font-size: 15px;
  line-height: 1.5;
  letter-spacing: var(--gh-tracking);
  outline: none;
  overflow: auto;
}
.g-modal--sm { max-width: 420px; }
.g-modal--md { max-width: 560px; }
.g-modal--lg { max-width: 760px; }

.g-modal__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 20px 20px 0 24px;
}
.g-modal__heading {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
  padding-top: 2px;
}
.g-modal__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  line-height: 1.25;
  font-weight: 700;
}
.g-modal__description {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.g-modal__body {
  padding: 16px 24px 24px;
}
.g-modal__footer {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 12px;
  padding: 16px 24px 20px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}

.g-modal__overlay[data-state='open'] { animation: g-modal-fade var(--gh-dur-base) var(--ease-gh); }
.g-modal__overlay[data-state='closed'] { animation: g-modal-fade var(--gh-dur-fast) var(--ease-gh) reverse; }
.g-modal[data-state='open'] { animation: g-modal-in var(--gh-dur-base) var(--ease-gh); }
.g-modal[data-state='closed'] { animation: g-modal-in var(--gh-dur-fast) var(--ease-gh) reverse; }
@keyframes g-modal-fade {
  from { opacity: 0; }
}
@keyframes g-modal-in {
  from { opacity: 0; transform: translate(-50%, calc(-50% + 8px)); }
}
@media (prefers-reduced-motion: reduce) {
  .g-modal__overlay[data-state],
  .g-modal[data-state] { animation: none; }
}
</style>
