<script setup lang="ts">
/**
 * "About {event}": the event's description, formatted, on the registration pages.
 * Long descriptions start cut to `collapsedHeight` with a fade and a Show more / Show less button.
 */
const props = withDefaults(defineProps<{
  name?: string | null
  description?: string | null
  /** px; descriptions taller than this start collapsed */
  collapsedHeight?: number
}>(), {
  collapsedHeight: 288,
})

const id = useId()
const body = ref<HTMLElement | null>(null)
const expanded = ref(false)
const overflowing = ref(false)

function measure() {
  if (body.value)
    overflowing.value = body.value.scrollHeight > props.collapsedHeight + 48
}

// the Markdown renders asynchronously, so measure whenever its size changes
let observer: ResizeObserver | null = null
onMounted(() => {
  observer = new ResizeObserver(measure)
  const content = body.value?.firstElementChild
  if (content)
    observer.observe(content)
  measure()
})
onBeforeUnmount(() => observer?.disconnect())
</script>

<template>
  <section
    v-if="description?.trim()"
    class="about"
    :aria-labelledby="`${id}-title`"
  >
    <h2
      :id="`${id}-title`"
      class="about__title"
    >
      {{ name ? `About ${name}` : 'About this event' }}
    </h2>
    <div
      :id="`${id}-body`"
      ref="body"
      class="about__body"
      :class="{ 'about__body--collapsed': overflowing && !expanded }"
      :style="overflowing && !expanded ? { maxHeight: `${collapsedHeight}px` } : undefined"
    >
      <GMarkdown
        :value="description"
        :heading-level="3"
      />
    </div>
    <GButton
      v-if="overflowing"
      variant="ghost"
      size="sm"
      :icon="expanded ? 'material-symbols:keyboard-arrow-up-rounded' : 'material-symbols:keyboard-arrow-down-rounded'"
      :label="expanded ? 'Show less' : 'Show more'"
      :aria-expanded="expanded"
      :aria-controls="`${id}-body`"
      class="self-start"
      @click="expanded = !expanded"
    />
  </section>
</template>

<style scoped>
.about {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
  padding: 16px 18px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
}
.about__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
  line-height: 1.25;
}
.about__body {
  position: relative;
  overflow: hidden;
  font-size: 15px;
}
/* cut-off text fades into the card */
.about__body--collapsed::after {
  content: '';
  position: absolute;
  inset: auto 0 0;
  height: 64px;
  background: linear-gradient(to bottom, transparent, var(--gh-surface));
  pointer-events: none;
}
</style>
