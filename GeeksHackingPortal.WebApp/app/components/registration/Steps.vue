<script setup lang="ts">
/**
 * Where a participant is in the hackathon sign-up: sign up → answer the questions →
 * organiser review → form a team. Done steps get a check, the current step is the yellow
 * block (aria-current="step"), a rejected review is marked in red with a cross.
 */
const props = defineProps<{
  /** 0-based index of the current step; steps before it are done. 4 = everything done. */
  current: number
  /** the review step (index 2) ended in a rejection */
  rejected?: boolean
}>()

const steps = ['Sign up', 'Answer the questions', 'Organiser review', 'Form a team']

function stateOf(index: number): 'done' | 'current' | 'failed' | 'todo' {
  if (props.rejected && index === 2)
    return 'failed'
  if (index < props.current)
    return 'done'
  if (index === props.current && !props.rejected)
    return 'current'
  return 'todo'
}

const srState = { done: 'done', current: 'current step', failed: 'not approved', todo: 'not started' }
</script>

<template>
  <ol
    class="reg-steps"
    aria-label="Registration progress"
  >
    <li
      v-for="(step, index) in steps"
      :key="step"
      class="reg-steps__item"
      :class="`reg-steps__item--${stateOf(index)}`"
      :aria-current="stateOf(index) === 'current' ? 'step' : undefined"
    >
      <span
        class="reg-steps__mark"
        aria-hidden="true"
      >
        <Icon
          v-if="stateOf(index) === 'done'"
          name="material-symbols:check-rounded"
        />
        <Icon
          v-else-if="stateOf(index) === 'failed'"
          name="material-symbols:close-rounded"
        />
        <template v-else>{{ index + 1 }}</template>
      </span>
      <span class="reg-steps__label">{{ step }}</span>
      <span class="sr-only">({{ srState[stateOf(index)] }})</span>
    </li>
  </ol>
</template>

<style scoped>
.reg-steps {
  display: flex;
  flex-direction: column;
  gap: 0;
  margin: 0;
  padding: 0;
  list-style: none;
}
.reg-steps__item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 6px 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
/* the rail between marks */
.reg-steps__item:not(:last-child)::after {
  content: '';
  position: absolute;
  top: calc(50% + 14px);
  left: 13px;
  width: 1.5px;
  height: calc(100% - 28px + 12px);
  background: var(--gh-line-soft);
}
.reg-steps__mark {
  display: grid;
  flex: none;
  place-items: center;
  width: 28px;
  height: 28px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-pill);
  background: var(--gh-surface);
  font-family: var(--font-display);
  font-size: 13px;
  font-weight: 700;
}
.reg-steps__mark :deep(svg),
.reg-steps__mark .iconify {
  width: 18px;
  height: 18px;
}
.reg-steps__item--done {
  color: var(--gh-text);
}
.reg-steps__item--done .reg-steps__mark {
  border-color: var(--gh-line);
  background: var(--gh-text);
  color: var(--gh-bg);
}
.reg-steps__item--current {
  color: var(--gh-text);
}
.reg-steps__item--current .reg-steps__mark {
  border-color: var(--color-gh-ink);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.reg-steps__item--current .reg-steps__label {
  font-weight: 700;
}
.reg-steps__item--failed {
  color: var(--gh-err);
}
.reg-steps__item--failed .reg-steps__mark {
  border-color: var(--gh-err);
  background: var(--gh-err);
  color: var(--gh-bg);
}
</style>
