<script setup lang="ts">
/**
 * GBadge: short status or count label.
 *
 * Tones: status is green / red only; "pending" (waiting on someone) is always solid yellow with
 * dark text, because yellow is never used as text or a thin line. Everything else is neutral.
 * Variants: soft (default; tinted in light, outlined in dark: no tints there), solid, outline.
 *
 * Migrating from <UBadge>:
 *   color   success → ok · error → err · warning → pending · neutral / primary / info → neutral
 *   variant subtle / soft → soft · solid → solid · outline → outline
 */
withDefaults(defineProps<{
  tone?: 'neutral' | 'ok' | 'err' | 'pending'
  variant?: 'soft' | 'solid' | 'outline'
  size?: 'sm' | 'md'
  label?: string | number
  icon?: string
}>(), {
  tone: 'neutral',
  variant: 'soft',
  size: 'md',
})
</script>

<template>
  <span
    class="g-badge"
    :class="[`g-badge--${tone}`, `g-badge--${tone === 'pending' ? 'solid' : variant}`, `g-badge--${size}`]"
  >
    <Icon
      v-if="icon"
      :name="icon"
      :key="icon"
      class="g-badge__icon"
      aria-hidden="true"
    />
    <slot>{{ label }}</slot>
  </span>
</template>

<style scoped>
.g-badge {
  --g-badge-fg: var(--gh-text);
  --g-badge-bg: transparent;
  --g-badge-border: var(--gh-line);

  display: inline-flex;
  align-items: center;
  gap: 4px;
  max-width: 100%;
  border: var(--gh-border-width) solid var(--g-badge-border);
  border-radius: var(--radius-gh-pill);
  background: var(--g-badge-bg);
  color: var(--g-badge-fg);
  font-family: var(--font-body);
  font-weight: 600;
  letter-spacing: var(--gh-tracking);
  line-height: 1.2;
  white-space: nowrap;
  vertical-align: middle;
}
.g-badge--sm { padding: 1px 8px; font-size: 12px; }
.g-badge--md { padding: 3px 10px; font-size: 13px; }
.g-badge__icon { width: 1.1em; height: 1.1em; flex: none; }

/* soft */
.g-badge--neutral.g-badge--soft { --g-badge-bg: var(--gh-surface-sunken); --g-badge-border: var(--gh-line-soft); }
.g-badge--ok.g-badge--soft { --g-badge-fg: var(--gh-ok); --g-badge-bg: var(--gh-ok-soft); --g-badge-border: var(--gh-ok); }
.g-badge--err.g-badge--soft { --g-badge-fg: var(--gh-err); --g-badge-bg: var(--gh-err-soft); --g-badge-border: var(--gh-err); }

/* solid */
.g-badge--neutral.g-badge--solid { --g-badge-fg: var(--gh-bg); --g-badge-bg: var(--gh-text); --g-badge-border: var(--gh-text); }
.g-badge--ok.g-badge--solid { --g-badge-fg: var(--gh-on-ok); --g-badge-bg: var(--gh-ok); --g-badge-border: var(--gh-ok); }
.g-badge--err.g-badge--solid { --g-badge-fg: var(--gh-on-err); --g-badge-bg: var(--gh-err); --g-badge-border: var(--gh-err); }
.g-badge--pending.g-badge--solid { --g-badge-fg: var(--gh-on-accent); --g-badge-bg: var(--gh-accent); --g-badge-border: var(--gh-on-accent); }

/* outline */
.g-badge--ok.g-badge--outline { --g-badge-fg: var(--gh-ok); --g-badge-border: var(--gh-ok); }
.g-badge--err.g-badge--outline { --g-badge-fg: var(--gh-err); --g-badge-border: var(--gh-err); }
</style>
