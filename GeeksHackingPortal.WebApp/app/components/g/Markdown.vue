<script setup lang="ts">
import type { Component } from 'vue'
import { Markdown } from '@comark/vue'
import breaks from '@comark/vue/plugins/breaks'

/**
 * GMarkdown: renders an event description (Markdown written with GMarkdownEditor) in the
 * portal's prose style (`.g-prose`, assets/css/prose.css).
 *
 * Safe for text anyone with organiser access typed:
 * - plain Markdown only: raw HTML (`<script>`, `<img onerror>`) and Comark's component syntax
 *   are not parsed, so they show as text
 * - links only to http(s), mailto or same-site paths; others render as plain text.
 *   Links to other sites open in a new tab.
 * - single line breaks are kept, as older plain-text descriptions rely on them
 * - headings are moved down to sit under the page's own heading: `#` and `##` become
 *   `<h{headingLevel}>`, `###` the level below
 */
const props = withDefaults(defineProps<{
  value?: string | null
  /** the HTML level for the description's top headings (`#` / `##`) */
  headingLevel?: 2 | 3 | 4
}>(), {
  headingLevel: 3,
})

const SafeLink = defineComponent({
  inheritAttrs: false,
  setup(_, { attrs, slots }) {
    return () => {
      const href = String(attrs.href ?? '').trim()
      const external = /^https?:\/\//i.test(href)
      const allowed = external || /^mailto:/i.test(href) || (/^\/(?!\/)/.test(href))
      if (!allowed)
        return h('span', slots.default?.())
      return h('a', external ? { href, target: '_blank', rel: 'noopener noreferrer' } : { href }, slots.default?.())
    }
  },
})

function heading(level: number): Component {
  const tag = `h${Math.min(level, 6)}`
  return defineComponent({
    inheritAttrs: false,
    setup: (_, { slots }) => () => h(tag, slots.default?.()),
  })
}

const components = computed(() => ({
  a: SafeLink,
  h1: heading(props.headingLevel),
  h2: heading(props.headingLevel),
  h3: heading(props.headingLevel + 1),
  h4: heading(props.headingLevel + 2),
  h5: heading(props.headingLevel + 2),
  h6: heading(props.headingLevel + 2),
}))

const options = { registerDefaultPlugins: false, autoClose: true, autoUnwrap: true, headingIds: false }
const plugins = [breaks()]
</script>

<template>
  <div class="g-prose">
    <Suspense>
      <Markdown
        :value="value ?? ''"
        :options="options"
        :plugins="plugins"
        :components="components"
      />
    </Suspense>
  </div>
</template>
