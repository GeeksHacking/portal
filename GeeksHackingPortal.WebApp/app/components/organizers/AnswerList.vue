<script setup lang="ts">
/**
 * One person's registration answers as organisers see them (hackathon review queue and
 * workshop participants): question, answer, then the follow-up text. Pass the submissions
 * already in question order. Answers keep their line breaks.
 */
export interface OrganizerAnswer {
  questionId?: string | null
  questionText?: string | null
  value?: string | null
  followUpValue?: string | null
}

defineProps<{
  answers: OrganizerAnswer[]
  emptyText?: string
}>()
</script>

<template>
  <dl
    v-if="answers.length"
    class="oa"
  >
    <div
      v-for="(answer, index) in answers"
      :key="answer.questionId ?? index"
      class="oa__row"
    >
      <dt>{{ answer.questionText || 'Question' }}</dt>
      <dd>
        <span
          v-if="answer.value?.trim()"
          class="oa__value"
        >{{ answer.value.trim() }}</span>
        <span
          v-else-if="!answer.followUpValue?.trim()"
          class="oa__none"
        >No answer</span>
        <span
          v-if="answer.followUpValue?.trim()"
          class="oa__followup"
        >
          <span class="oa__followup-label">Follow-up</span>
          {{ answer.followUpValue.trim() }}
        </span>
      </dd>
    </div>
  </dl>
  <p
    v-else
    class="oa__empty"
  >
    {{ emptyText ?? 'No answers yet.' }}
  </p>
</template>

<style scoped>
.oa {
  display: flex;
  flex-direction: column;
  margin: 0;
}
.oa__row {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 12px 0;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.oa__row:first-child {
  padding-top: 0;
  border-top: 0;
}
.oa__row:last-child { padding-bottom: 0; }
.oa__row dt {
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.oa__row dd {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  overflow-wrap: anywhere;
}
.oa__value { white-space: pre-line; }
.oa__none,
.oa__empty {
  margin: 0;
  color: var(--gh-text-muted);
}
.oa__followup {
  padding-left: 12px;
  border-left: 3px solid var(--gh-line-soft);
  font-size: 14px;
  white-space: pre-line;
}
.oa__followup-label {
  display: block;
  font-size: 12px;
  color: var(--gh-text-muted);
}
</style>
