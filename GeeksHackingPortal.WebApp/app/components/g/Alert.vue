<script setup lang="ts">
/**
 * GAlert: an inline message about the page or a form.
 *
 * Tones: neutral (info), ok, err, attention (needs the reader to act; the one place a pale yellow
 * fill appears in light mode, a yellow edge in dark). Body text is always the normal text colour,
 * so only the icon and edge carry the tone.
 * `err` alerts use role="alert" so they are announced; the rest use role="status".
 * Pass `dismissible` to show a close button; it emits `close` (hiding it is up to you).
 *
 * Migrating from <UAlert>: color error → err · success → ok · warning → attention · others → neutral.
 */
const props = withDefaults(defineProps<{
  tone?: 'neutral' | 'ok' | 'err' | 'attention'
  title?: string
  description?: string
  /** Defaults to an icon per tone; pass null for none. */
  icon?: string | null
  dismissible?: boolean
}>(), {
  tone: 'neutral',
})

const emit = defineEmits<{ close: [] }>()

const defaultIcons = {
  neutral: 'material-symbols:info-rounded',
  ok: 'material-symbols:check-circle-rounded',
  err: 'material-symbols:error-rounded',
  attention: 'material-symbols:warning-rounded',
} as const

const iconName = computed(() => (props.icon === null ? undefined : props.icon ?? defaultIcons[props.tone]))
</script>

<template>
  <div
    class="g-alert"
    :class="`g-alert--${tone}`"
    :role="tone === 'err' ? 'alert' : 'status'"
  >
    <Icon
      v-if="iconName"
      :name="iconName"
      :key="iconName"
      class="g-alert__icon"
      aria-hidden="true"
    />
    <div class="g-alert__text">
      <p
        v-if="title"
        class="g-alert__title"
      >
        {{ title }}
      </p>
      <p
        v-if="description"
        class="g-alert__description"
      >
        {{ description }}
      </p>
      <div
        v-if="$slots.default"
        class="g-alert__description"
      >
        <slot />
      </div>
      <div
        v-if="$slots.actions"
        class="g-alert__actions"
      >
        <slot name="actions" />
      </div>
    </div>
    <GButton
      v-if="dismissible"
      class="g-alert__close"
      variant="ghost"
      size="sm"
      icon="material-symbols:close-rounded"
      aria-label="Dismiss"
      @click="emit('close')"
    />
  </div>
</template>

<style scoped>
.g-alert {
  --g-alert-bg: var(--gh-surface);
  --g-alert-edge: var(--gh-line);
  --g-alert-icon: var(--gh-text);

  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  border: var(--gh-border-width) solid var(--g-alert-edge);
  border-left-width: 6px;
  border-radius: var(--radius-gh);
  background: var(--g-alert-bg);
  color: var(--gh-text);
  font-family: var(--font-body);
  font-size: 14px;
  line-height: 1.5;
  letter-spacing: var(--gh-tracking);
}
.g-alert--ok { --g-alert-bg: var(--gh-ok-soft); --g-alert-edge: var(--gh-ok); --g-alert-icon: var(--gh-ok); }
.g-alert--err { --g-alert-bg: var(--gh-err-soft); --g-alert-edge: var(--gh-err); --g-alert-icon: var(--gh-err); }
.g-alert--attention { --g-alert-bg: var(--gh-highlight); --g-alert-edge: var(--gh-highlight-edge); --g-alert-icon: var(--gh-highlight-ink); }

.g-alert__icon {
  flex: none;
  width: 20px;
  height: 20px;
  margin-top: 1px;
  color: var(--g-alert-icon);
}
.g-alert__text {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.g-alert__title {
  margin: 0;
  font-weight: 700;
}
.g-alert__description {
  margin: 0;
  color: var(--gh-text-muted);
}
.g-alert__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 10px;
}
.g-alert__close {
  margin: -4px -6px -4px 0;
}
</style>
