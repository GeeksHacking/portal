<script setup lang="ts">
/**
 * GSectionHeading: big section numeral (B accent) + label strip + heading, with a rule above.
 * For long pages split into numbered steps or sections (registration, event portal, design lab).
 *
 * Give the section `aria-labelledby` pointing at `id`. The numeral is decorative and hidden from
 * screen readers; put the step number in the title or label if it matters.
 */
const props = withDefaults(defineProps<{
  title: string
  numeral?: string | number
  label?: string
  level?: 1 | 2 | 3
  id?: string
}>(), {
  level: 2,
})

const numeralText = computed(() => (typeof props.numeral === 'number' ? String(props.numeral).padStart(2, '0') : props.numeral))
</script>

<template>
  <div class="g-section">
    <span
      v-if="numeralText"
      class="g-section__numeral"
      aria-hidden="true"
    >{{ numeralText }}</span>
    <div class="g-section__text">
      <GLabelStrip
        v-if="label"
        :label="label"
        class="g-section__strip"
      />
      <component
        :is="`h${level}`"
        :id="id"
        class="g-section__title"
        :class="`g-section__title--h${level}`"
      >
        {{ title }}
      </component>
      <p
        v-if="$slots.default"
        class="g-section__description"
      >
        <slot />
      </p>
    </div>
  </div>
</template>

<style scoped>
.g-section {
  display: flex;
  align-items: flex-end;
  gap: 20px;
  padding-top: 12px;
  border-top: 2px solid var(--gh-text);
  color: var(--gh-text);
}
.g-section__numeral {
  flex: none;
  font-family: var(--font-display);
  font-size: 72px;
  font-weight: 600;
  line-height: 0.8;
}
.g-section__text {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  min-width: 0;
}
.g-section__title {
  margin: 0;
  font-family: var(--font-display);
  font-weight: 700;
  letter-spacing: var(--gh-tracking);
}
.g-section__title--h1 { font-size: 45px; line-height: 1.1; }
.g-section__title--h2 { font-size: 32px; line-height: 1.15; }
.g-section__title--h3 { font-size: 22px; line-height: 1.3; }
.g-section__description {
  max-width: 70ch;
  margin: 0;
  font-family: var(--font-body);
  font-size: 15px;
  color: var(--gh-text-muted);
}
@media (max-width: 640px) {
  .g-section { gap: 14px; }
  .g-section__numeral { font-size: 52px; }
  .g-section__title--h1 { font-size: 34px; }
  .g-section__title--h2 { font-size: 26px; }
}
</style>
