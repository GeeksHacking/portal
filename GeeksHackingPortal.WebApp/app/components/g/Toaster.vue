<script setup lang="ts">
import type { GToast } from '~/composables/useGToast'
import {
  ToastAction,
  ToastClose,
  ToastDescription,
  ToastProvider,
  ToastRoot,
  ToastTitle,
  ToastViewport,
} from 'reka-ui'

/**
 * GToaster: renders the toasts queued with useGToast(). Mount it once (app.vue).
 * Bottom-right on desktop, full width at the bottom on phones. F8 jumps to the toasts.
 */
const { toasts, dismiss, remove } = useGToast()

const icons = {
  neutral: 'i-lucide-info',
  ok: 'i-lucide-circle-check',
  err: 'i-lucide-circle-alert',
  attention: 'i-lucide-triangle-alert',
} as const

function iconFor(toast: GToast) {
  return toast.icon === null ? undefined : toast.icon ?? icons[toast.tone]
}

function onOpenChange(id: string, open: boolean) {
  if (open)
    return
  dismiss(id)
  // let the close animation finish before dropping it from the queue
  setTimeout(() => remove(id), 300)
}
</script>

<template>
  <ToastProvider
    swipe-direction="right"
    label="Notification"
  >
    <ToastRoot
      v-for="toast in toasts"
      :key="toast.id"
      :open="toast.open"
      :duration="toast.duration ?? (toast.tone === 'err' ? 8000 : 5000)"
      :type="toast.tone === 'err' ? 'foreground' : 'background'"
      class="g-toast"
      :class="`g-toast--${toast.tone}`"
      @update:open="onOpenChange(toast.id, $event)"
    >
      <Icon
        v-if="iconFor(toast)"
        :name="iconFor(toast)!"
        class="g-toast__icon"
        aria-hidden="true"
      />
      <div class="g-toast__text">
        <ToastTitle class="g-toast__title">
          {{ toast.title }}
        </ToastTitle>
        <ToastDescription
          v-if="toast.description"
          class="g-toast__description"
        >
          {{ toast.description }}
        </ToastDescription>
        <ToastAction
          v-if="toast.action"
          :alt-text="toast.action.label"
          as-child
          class="g-toast__action"
        >
          <GButton
            size="sm"
            :label="toast.action.label"
            @click="toast.action.onClick()"
          />
        </ToastAction>
      </div>
      <ToastClose as-child>
        <GButton
          variant="ghost"
          size="sm"
          icon="i-lucide-x"
          aria-label="Dismiss notification"
          class="g-toast__close"
        />
      </ToastClose>
    </ToastRoot>
    <ToastViewport class="g-toaster" />
  </ToastProvider>
</template>

<style>
.g-toaster {
  position: fixed;
  right: 0;
  bottom: 0;
  z-index: 100;
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: min(400px, 100vw);
  max-height: 100dvh;
  margin: 0;
  padding: 16px;
  list-style: none;
  outline: none;
  overflow: hidden;
  pointer-events: none;
}
.g-toaster:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: -3px;
}

.g-toast {
  --g-toast-edge: var(--gh-line);
  --g-toast-icon: var(--gh-text);

  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 12px 12px 14px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-left: 6px solid var(--g-toast-edge);
  border-radius: var(--radius-gh);
  background: var(--gh-surface-raised);
  color: var(--gh-text);
  box-shadow: var(--gh-shadow-overlay);
  font-family: var(--font-body);
  font-size: 14px;
  line-height: 1.45;
  letter-spacing: var(--gh-tracking);
  pointer-events: auto;
}
.g-toast--ok { --g-toast-edge: var(--gh-ok); --g-toast-icon: var(--gh-ok); }
.g-toast--err { --g-toast-edge: var(--gh-err); --g-toast-icon: var(--gh-err); }
.g-toast--attention { --g-toast-edge: var(--gh-highlight-edge); --g-toast-icon: var(--gh-highlight-ink); }

.g-toast__icon {
  flex: none;
  width: 20px;
  height: 20px;
  margin-top: 1px;
  color: var(--g-toast-icon);
}
.g-toast__text {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.g-toast__title {
  font-weight: 700;
}
.g-toast__description {
  color: var(--gh-text-muted);
}
.g-toast__action {
  align-self: flex-start;
  margin-top: 8px;
}
.g-toast__close {
  margin: -4px -4px -4px 0;
}

.g-toast[data-state='open'] { animation: g-toast-in var(--gh-dur-base) var(--ease-gh); }
.g-toast[data-state='closed'] { animation: g-toast-out var(--gh-dur-fast) var(--ease-gh) forwards; }
.g-toast[data-swipe='move'] { transform: translateX(var(--reka-toast-swipe-move-x)); }
.g-toast[data-swipe='cancel'] { transform: translateX(0); transition: transform var(--gh-dur-fast) var(--ease-gh); }
.g-toast[data-swipe='end'] { animation: g-toast-swipe var(--gh-dur-fast) var(--ease-gh) forwards; }
@keyframes g-toast-in { from { opacity: 0; transform: translateY(12px); } }
@keyframes g-toast-out { to { opacity: 0; } }
@keyframes g-toast-swipe {
  from { transform: translateX(var(--reka-toast-swipe-end-x)); }
  to { transform: translateX(100%); opacity: 0; }
}
@media (prefers-reduced-motion: reduce) {
  .g-toast[data-state],
  .g-toast[data-swipe] { animation: none; transition: none; }
}
</style>
