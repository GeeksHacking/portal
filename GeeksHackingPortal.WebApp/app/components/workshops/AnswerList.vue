<script setup lang="ts">
import type { WorkshopAnswerGroup } from '~/composables/useWorkshopAnswers'

/**
 * A participant's saved workshop answers (from useWorkshopAnswers): one card per category,
 * question on the left and answer on the right (stacked on phones), follow-up texts under the
 * answer. With no answers, a note that the workshop asked none.
 */
withDefaults(defineProps<{
  groups: WorkshopAnswerGroup[]
  /** heading level for the category names */
  headingLevel?: 2 | 3
}>(), {
  headingLevel: 3,
})
</script>

<template>
  <div class="ws-answers">
    <template v-if="groups.length">
      <div
        v-for="group in groups"
        :key="group.name"
        class="ws-answers__card"
      >
        <component
          :is="`h${headingLevel}`"
          class="ws-answers__group"
        >
          {{ group.name }}
        </component>
        <dl class="ws-answers__list">
          <div
            v-for="detail in group.details"
            :key="detail.key"
            class="ws-answers__row"
          >
            <dt>{{ detail.question }}</dt>
            <dd>
              <span>{{ detail.answers.join(', ') }}</span>
              <span
                v-for="followUp in detail.followUps"
                :key="`${detail.key}-${followUp.label}`"
                class="ws-answers__followup"
              >
                <span class="ws-answers__followup-label">{{ followUp.label }}</span>
                {{ followUp.value }}
              </span>
            </dd>
          </div>
        </dl>
      </div>
    </template>
    <GAlert
      v-else
      title="No answers to show"
      description="This workshop didn't ask any registration questions."
    />
  </div>
</template>

<style scoped>
.ws-answers {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.ws-answers__card {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
@media (min-width: 640px) {
  .ws-answers__card { padding: 24px; }
}
.ws-answers__group {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.ws-answers__list {
  display: flex;
  flex-direction: column;
  margin: 0;
}
.ws-answers__row {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 4px 24px;
  padding: 12px 0;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.ws-answers__row:first-child {
  padding-top: 0;
  border-top: 0;
}
.ws-answers__row:last-child {
  padding-bottom: 0;
}
.ws-answers__row dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
@media (min-width: 768px) {
  .ws-answers__row { grid-template-columns: minmax(0, 2fr) minmax(0, 3fr); }
  .ws-answers__row dt {
    font-size: 14px;
    font-weight: 600;
    letter-spacing: normal;
    text-transform: none;
    color: var(--gh-text);
  }
}
.ws-answers__row dd {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  overflow-wrap: anywhere;
}
.ws-answers__followup {
  padding-left: 12px;
  border-left: 3px solid var(--gh-line-soft);
  font-size: 14px;
}
.ws-answers__followup-label {
  display: block;
  font-size: 12px;
  color: var(--gh-text-muted);
}
</style>
