<script setup lang="ts">
/**
 * The frame for the full-page event forms (create / edit a hackathon or workshop).
 *
 * - A "Back to workspace" link, the page title, and on wide screens a list of the form's sections
 *   that jumps to each one.
 * - The form itself (default slot, made of <OrganizersFormSection>s) and a save bar that stays at the
 *   bottom of the screen with Cancel and Save, plus a summary when saving failed.
 * - Leaving with unsaved changes (`dirty`) asks first, both inside the app and when closing the tab.
 *   The page clears `dirty` (e.g. by resetting its snapshot) before navigating away after a save.
 */
const props = defineProps<{
  title: string
  /** small label above the title, e.g. "Hackathon" */
  eyebrow?: string
  sections: { id: string, label: string }[]
  saveLabel: string
  saving?: boolean
  dirty?: boolean
  /** shown above the save bar after a failed save */
  error?: string
  /** where Cancel and Back go */
  backTo?: string
}>()

const emit = defineEmits<{ submit: [] }>()

const formId = `event-form-${useId()}`
const backTo = computed(() => props.backTo ?? '/dash/manage')

// ---- which section is in view (highlighted in the section list) -------------------------------
const root = ref<HTMLElement | null>(null)
const activeId = ref(props.sections[0]?.id ?? '')
let scroller: HTMLElement | null = null
// after a click, keep the clicked item lit until the smooth scroll settles
let lockedUntil = 0
let frame = 0

// current = the last section whose top has passed the upper third of the panel;
// at the very bottom the last section wins (the final sections are short)
function pickActive() {
  frame = 0
  if (!scroller || Date.now() < lockedUntil)
    return
  if (scroller.scrollTop + scroller.clientHeight >= scroller.scrollHeight - 4) {
    activeId.value = props.sections.at(-1)?.id ?? activeId.value
    return
  }
  const line = scroller.getBoundingClientRect().top + scroller.clientHeight / 3
  let current = props.sections[0]?.id ?? ''
  for (const section of props.sections) {
    const el = document.getElementById(section.id)
    if (el && el.getBoundingClientRect().top <= line)
      current = section.id
  }
  activeId.value = current
}

function onScroll() {
  if (!frame)
    frame = requestAnimationFrame(pickActive)
}

onMounted(() => {
  scroller = root.value?.closest<HTMLElement>('.g-panel__body') ?? null
  scroller?.addEventListener('scroll', onScroll, { passive: true })
  pickActive()
})

onBeforeUnmount(() => {
  scroller?.removeEventListener('scroll', onScroll)
  cancelAnimationFrame(frame)
})

function jumpTo(id: string) {
  activeId.value = id
  lockedUntil = Date.now() + 800
  const target = document.getElementById(id)
  target?.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' })
  // move focus to the section heading so keyboard and screen-reader users land there too
  target?.querySelector<HTMLElement>('h2')?.focus({ preventScroll: true })
}

// ---- unsaved changes ----------------------------------------------------------------------------
const isLeaveOpen = ref(false)
const pendingTo = ref<string | null>(null)
let leaving = false

onBeforeRouteLeave((to) => {
  if (!props.dirty || leaving)
    return true
  pendingTo.value = to.fullPath
  isLeaveOpen.value = true
  return false
})

function discardAndLeave() {
  leaving = true
  isLeaveOpen.value = false
  if (pendingTo.value)
    navigateTo(pendingTo.value)
}

function onBeforeUnload(event: BeforeUnloadEvent) {
  if (props.dirty) {
    event.preventDefault()
    event.returnValue = ''
  }
}
onMounted(() => window.addEventListener('beforeunload', onBeforeUnload))
onBeforeUnmount(() => window.removeEventListener('beforeunload', onBeforeUnload))
</script>

<template>
  <div
    ref="root"
    class="ef"
  >
    <div class="ef-head">
      <GButton
        :to="backTo"
        variant="ghost"
        size="sm"
        icon="material-symbols:arrow-back-rounded"
        label="Back to workspace"
        class="self-start"
      />
      <p
        v-if="eyebrow"
        class="ef-head__eyebrow"
      >
        {{ eyebrow }}
      </p>
      <h1 class="ef-head__title">
        {{ title }}
      </h1>
    </div>

    <div class="ef-body">
      <nav
        class="ef-nav"
        aria-label="Form sections"
      >
        <ol>
          <li
            v-for="section in sections"
            :key="section.id"
          >
            <button
              type="button"
              class="ef-nav__link"
              :aria-current="activeId === section.id ? 'location' : undefined"
              @click="jumpTo(section.id)"
            >
              {{ section.label }}
            </button>
          </li>
        </ol>
      </nav>

      <form
        :id="formId"
        class="ef-form"
        novalidate
        @submit.prevent="emit('submit')"
      >
        <slot />
      </form>
    </div>

    <!-- always in view: the way out and the way to save -->
    <div class="ef-bar">
      <div class="ef-bar__inner">
        <p
          v-if="error"
          class="ef-bar__error"
          role="alert"
        >
          <Icon
            name="material-symbols:error-rounded"
            aria-hidden="true"
          />
          <span>{{ error }}</span>
        </p>
        <p
          v-else-if="dirty"
          class="ef-bar__note"
        >
          Unsaved changes
        </p>
        <div class="ef-bar__actions">
          <GButton
            :to="backTo"
            variant="ghost"
            label="Cancel"
          />
          <GButton
            type="submit"
            :form="formId"
            variant="primary"
            icon="material-symbols:check-rounded"
            :label="saveLabel"
            :loading="saving"
            loading-label="Saving"
          />
        </div>
      </div>
    </div>
  </div>

  <GModal
    v-model:open="isLeaveOpen"
    title="Leave without saving?"
    description="Your changes to this event will be lost."
    size="sm"
  >
    <template #footer>
      <GButton
        variant="ghost"
        label="Keep editing"
        @click="isLeaveOpen = false"
      />
      <GButton
        variant="danger"
        label="Discard changes"
        @click="discardAndLeave"
      />
    </template>
  </GModal>
</template>

<style scoped>
.ef {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  min-height: 100%;
}
/* the bar spans the panel; the content keeps a readable width */
.ef-head,
.ef-body {
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.ef-head {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.ef-head__eyebrow {
  margin: 8px 0 0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ef-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
  overflow-wrap: anywhere;
}

.ef-body {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 32px;
  flex: 1 1 auto;
}
.ef-nav { display: none; }
@media (min-width: 1024px) {
  .ef-body { grid-template-columns: 11rem minmax(0, 1fr); }
  .ef-nav { display: block; }
}
.ef-nav ol {
  position: sticky;
  top: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.ef-nav__link {
  width: 100%;
  padding: 8px 12px;
  border: var(--gh-border-width) solid transparent;
  border-radius: var(--radius-gh-sm);
  background: none;
  color: var(--gh-text-muted);
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  text-align: left;
  cursor: pointer;
}
.ef-nav__link:hover {
  background: var(--gh-ghost-hover);
  color: var(--gh-text);
}
/* the section in view: the same yellow block as the sidebar's current page */
.ef-nav__link[aria-current='location'] {
  border-color: var(--gh-on-accent);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.ef-nav__link:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}

.ef-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}

/* sticks to the bottom of the scrolling panel; the offset cancels the panel's padding */
.ef-bar {
  position: sticky;
  bottom: -16px;
  z-index: 5;
  margin: 0 -16px -16px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-bg);
}
@media (min-width: 640px) {
  .ef-bar { bottom: -24px; margin: 0 -24px -24px; padding: 12px 24px; }
}
.ef-bar__inner {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 8px 16px;
  max-width: 64rem;
  margin: 0 auto;
}
.ef-bar__error,
.ef-bar__note {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  flex: 1 1 16rem;
  min-width: 0;
  margin: 0;
  font-size: 14px;
}
.ef-bar__error {
  color: var(--gh-err);
  font-weight: 600;
}
.ef-bar__error > :first-child {
  flex: none;
  width: 18px;
  height: 18px;
  margin-top: 1px;
}
.ef-bar__note { color: var(--gh-text-muted); }
.ef-bar__actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}
</style>
