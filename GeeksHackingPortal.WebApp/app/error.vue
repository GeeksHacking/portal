<script setup lang="ts">
import type { NuxtError } from '#app'

/**
 * Error page (Nuxt renders it instead of app.vue, so no layout or portal shell around it).
 * The status code is the big numeral; the copy says what happened in plain words and what to do.
 * Raw messages are only shown for unexpected errors, under "Details".
 */
const props = defineProps<{
  error: NuxtError
}>()

const code = computed(() => props.error?.statusCode ?? 500)

const copy = computed(() => {
  switch (code.value) {
    case 404:
      return { title: 'Page not found', body: 'This page doesn\'t exist or has moved. Check the link, or head back to the portal.' }
    case 401:
      return { title: 'Please sign in', body: 'Your session has ended. Sign in again to carry on.' }
    case 403:
      return { title: 'You don\'t have access', body: 'This page is only for some accounts. If you think you should see it, contact the organisers.' }
    default:
      return { title: 'Something went wrong', body: 'We couldn\'t load this page. Try again in a moment.' }
  }
})

const details = computed(() => {
  if ([401, 403, 404].includes(code.value))
    return null
  return props.error?.statusMessage || props.error?.message || null
})

useHead({ title: computed(() => `${copy.value.title} - GeeksHacking`) })

function goHome() {
  clearError({ redirect: code.value === 401 ? '/login' : '/' })
}
function retry() {
  window.location.reload()
}
</script>

<template>
  <main class="gh-error">
    <div class="gh-error__card">
      <GLogo
        :wordmark="false"
        size="md"
        decorative
      />
      <div class="gh-error__head">
        <span
          class="gh-error__code"
          aria-hidden="true"
        >{{ code }}</span>
        <div class="gh-error__titles">
          <GLabelStrip
            size="sm"
            :label="`Error ${code}`"
          />
          <h1 class="gh-error__title">
            {{ copy.title }}
          </h1>
        </div>
      </div>
      <p class="gh-error__body">
        {{ copy.body }}
      </p>
      <details
        v-if="details"
        class="gh-error__details"
      >
        <summary>Details</summary>
        <code>{{ details }}</code>
      </details>
      <div class="gh-error__actions">
        <GButton
          variant="primary"
          :label="code === 401 ? 'Sign in' : 'Back to the portal'"
          @click="goHome"
        />
        <GButton
          v-if="code >= 500"
          variant="secondary"
          icon="material-symbols:refresh-rounded"
          label="Try again"
          @click="retry"
        />
      </div>
    </div>
  </main>
</template>

<style scoped>
.gh-error {
  display: grid;
  place-items: center;
  min-height: 100dvh;
  padding: 24px 16px;
  background: var(--gh-bg);
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.gh-error__card {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 20px;
  width: 100%;
  max-width: 520px;
  padding: 32px 28px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.gh-error__head {
  display: flex;
  align-items: flex-end;
  gap: 18px;
  padding-top: 12px;
  border-top: 2px solid var(--gh-text);
  width: 100%;
}
.gh-error__code {
  flex: none;
  font-family: var(--font-display);
  font-size: 72px;
  font-weight: 600;
  line-height: 0.8;
}
.gh-error__titles {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  min-width: 0;
}
.gh-error__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 28px;
  font-weight: 700;
  line-height: 1.15;
}
.gh-error__body {
  margin: 0;
  max-width: 60ch;
  font-size: 15px;
  line-height: 1.55;
  color: var(--gh-text-muted);
}
.gh-error__details {
  width: 100%;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.gh-error__details summary {
  cursor: pointer;
  font-weight: 600;
  color: var(--gh-text);
}
.gh-error__details summary:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
  border-radius: 2px;
}
.gh-error__details code {
  display: block;
  margin-top: 8px;
  padding: 10px 12px;
  overflow-wrap: anywhere;
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface-sunken);
  color: var(--gh-text);
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: 0;
}
.gh-error__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
@media (max-width: 480px) {
  .gh-error__code { font-size: 56px; }
  .gh-error__title { font-size: 24px; }
}
</style>
