<script setup lang="ts">
import type { ChainedCommands, Editor } from '@tiptap/vue-3'
import Placeholder from '@tiptap/extension-placeholder'
import { Markdown } from '@tiptap/markdown'
import StarterKit from '@tiptap/starter-kit'
import { EditorContent, useEditor } from '@tiptap/vue-3'

/**
 * GMarkdownEditor: rich-text editing that reads and writes Markdown (TipTap underneath).
 *
 * - `v-model` is a Markdown string; '' when empty. Loading a value doesn't emit, so a form
 *   only turns dirty when someone edits.
 * - Toolbar: bold, italic, strikethrough, heading, small heading, bullet and numbered lists,
 *   quote, divider (horizontal rule), link, inline code, undo/redo. One tab stop; arrow keys
 *   move along it (WAI-ARIA toolbar). Ctrl/⌘+B, I, Shift+S, K (link), Z, Shift+Z work in the text.
 * - "Markdown" shows the raw Markdown in a textarea for people who prefer typing it.
 * - Only http(s) and mailto links. Pasted HTML is reduced to what the toolbar can make.
 * - Inside <GFormField> it is named by the field's label and picks up its description,
 *   invalid, required and disabled state.
 * Render the result with <GMarkdown>; both use the `.g-prose` styles.
 */
defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  placeholder?: string
  invalid?: boolean
  disabled?: boolean
  required?: boolean
  id?: string
  /** height of the writing area when empty */
  minRows?: number
}>(), {
  placeholder: '',
  minRows: 8,
})

const model = defineModel<string | null>({ default: '' })

const attrs = useAttrs()
const { field, invalid, disabled, required, describedBy } = useGFieldControl(props)

const mode = ref<'write' | 'markdown'>('write')
const isMac = import.meta.client && /Mac|iPhone|iPad/.test(navigator.platform)
const mod = isMac ? '⌘' : 'Ctrl+'

// ---- Markdown in and out --------------------------------------------------------------------
// the last Markdown this editor emitted, so our own updates coming back through v-model
// don't reset the content (and the cursor)
let lastEmitted: string | null = null

// tidy what TipTap writes: no trailing whitespace, at most one blank line between blocks
// (empty paragraphs come out as runs of blank lines), except inside code fences
function normalise(markdown: string) {
  const text = markdown
    .replace(/\s+$/, '')
    .split(/(^```[\s\S]*?^```)/m)
    .map((part, index) => (index % 2 ? part : part.replace(/\n{3,}/g, '\n\n')))
    .join('')
  return text.replace(/&nbsp;|\u00A0/g, '').trim() ? text : ''
}

function isSafeUrl(url: string) {
  return /^(?:https?:\/\/|mailto:)/i.test(url.trim())
}

const editor = useEditor({
  content: model.value ?? '',
  contentType: 'markdown',
  editable: !disabled.value,
  extensions: [
    StarterKit.configure({
      heading: { levels: [2, 3] },
      underline: false, // Markdown has no underline
      link: {
        openOnClick: false,
        autolink: true,
        linkOnPaste: true,
        defaultProtocol: 'https',
        isAllowedUri: (url, { defaultValidate }) => isSafeUrl(url) && !!defaultValidate(url),
        HTMLAttributes: { target: null, rel: null },
      },
    }),
    Markdown.configure({ markedOptions: { gfm: true, breaks: true } }),
    Placeholder.configure({ placeholder: () => props.placeholder }),
  ],
  onUpdate: ({ editor }) => {
    const markdown = normalise(editor.getMarkdown())
    lastEmitted = markdown
    model.value = markdown
  },
})

watch(model, (value) => {
  const next = value ?? ''
  if (!editor.value || next === lastEmitted)
    return
  lastEmitted = null
  editor.value.commands.setContent(next, { contentType: 'markdown', emitUpdate: false })
})

watch(disabled, value => editor.value?.setEditable(!value))

// the writing area is a contenteditable <div>, so name and describe it with ARIA
const contentAttrs = computed(() => {
  const labelledBy = !attrs['aria-label'] && field ? `${field.id.value}-label` : undefined
  return {
    'class': 'g-mde__content g-prose',
    'role': 'textbox',
    'aria-multiline': 'true',
    ...(labelledBy ? { 'aria-labelledby': labelledBy } : {}),
    ...(attrs['aria-label'] ? { 'aria-label': String(attrs['aria-label']) } : {}),
    ...(describedBy.value ? { 'aria-describedby': describedBy.value } : {}),
    ...(invalid.value ? { 'aria-invalid': 'true' } : {}),
    ...(required.value ? { 'aria-required': 'true' } : {}),
    'style': `min-height: calc(${props.minRows} * 1.7em + 24px)`,
  } as Record<string, string>
})
watch([editor, contentAttrs], ([instance, value]) => {
  instance?.setOptions({ editorProps: { attributes: value } })
}, { immediate: true })

// ---- toolbar --------------------------------------------------------------------------------
interface Tool {
  key: string
  label: string
  icon: string
  shortcut?: string
  /** a toggle shows its state with aria-pressed */
  isActive?: (e: Editor) => boolean
  can?: (e: Editor) => boolean
  run: (chain: ChainedCommands) => ChainedCommands
  /** opens the link dialog instead of running a command */
  link?: boolean
  /** starts a new group (drawn as a divider) */
  group?: boolean
}

const tools: Tool[] = [
  { key: 'bold', label: 'Bold', icon: 'material-symbols:format-bold-rounded', shortcut: `${mod}B`, isActive: e => e.isActive('bold'), run: c => c.toggleBold() },
  { key: 'italic', label: 'Italic', icon: 'material-symbols:format-italic-rounded', shortcut: `${mod}I`, isActive: e => e.isActive('italic'), run: c => c.toggleItalic() },
  { key: 'strike', label: 'Strikethrough', icon: 'material-symbols:format-strikethrough-rounded', shortcut: isMac ? '⇧⌘S' : 'Ctrl+Shift+S', isActive: e => e.isActive('strike'), run: c => c.toggleStrike() },
  { key: 'h2', label: 'Heading', icon: 'material-symbols:format-h2-rounded', group: true, isActive: e => e.isActive('heading', { level: 2 }), run: c => c.toggleHeading({ level: 2 }) },
  { key: 'h3', label: 'Small heading', icon: 'material-symbols:format-h3-rounded', isActive: e => e.isActive('heading', { level: 3 }), run: c => c.toggleHeading({ level: 3 }) },
  { key: 'ul', label: 'Bullet list', icon: 'material-symbols:format-list-bulleted-rounded', group: true, isActive: e => e.isActive('bulletList'), run: c => c.toggleBulletList() },
  { key: 'ol', label: 'Numbered list', icon: 'material-symbols:format-list-numbered-rounded', isActive: e => e.isActive('orderedList'), run: c => c.toggleOrderedList() },
  { key: 'quote', label: 'Quote', icon: 'material-symbols:format-quote-rounded', isActive: e => e.isActive('blockquote'), run: c => c.toggleBlockquote() },
  { key: 'hr', label: 'Divider', icon: 'material-symbols:horizontal-rule-rounded', can: e => e.can().setHorizontalRule(), run: c => c.setHorizontalRule() },
  { key: 'link', label: 'Link', icon: 'material-symbols:link-rounded', shortcut: `${mod}K`, group: true, link: true, isActive: e => e.isActive('link'), run: c => c },
  { key: 'code', label: 'Code', icon: 'material-symbols:code-rounded', shortcut: `${mod}E`, isActive: e => e.isActive('code'), run: c => c.toggleCode() },
  { key: 'undo', label: 'Undo', icon: 'material-symbols:undo-rounded', shortcut: `${mod}Z`, group: true, can: e => e.can().undo(), run: c => c.undo() },
  { key: 'redo', label: 'Redo', icon: 'material-symbols:redo-rounded', shortcut: isMac ? '⇧⌘Z' : 'Ctrl+Shift+Z', can: e => e.can().redo(), run: c => c.redo() },
]

function isDisabled(tool: Tool) {
  return disabled.value || !editor.value || (tool.can ? !tool.can(editor.value) : false)
}

function runTool(tool: Tool) {
  if (!editor.value || isDisabled(tool))
    return
  if (tool.link)
    return openLink()
  tool.run(editor.value.chain().focus()).run()
}

// roving tab stop: Tab lands on one button, arrows move along the toolbar
const toolbar = ref<HTMLElement | null>(null)
const focusIndex = ref(0)
function onToolbarKeydown(event: KeyboardEvent) {
  const buttons = [...(toolbar.value?.querySelectorAll<HTMLButtonElement>('.g-mde__tool') ?? [])]
  const current = buttons.indexOf(event.target as HTMLButtonElement)
  if (current < 0)
    return
  const step = { ArrowRight: 1, ArrowLeft: -1 }[event.key]
  let next = current
  if (step)
    next = (current + step + buttons.length) % buttons.length
  else if (event.key === 'Home')
    next = 0
  else if (event.key === 'End')
    next = buttons.length - 1
  else
    return
  event.preventDefault()
  focusIndex.value = next
  buttons[next]?.focus()
}

// ---- links ------------------------------------------------------------------------------------
const linkOpen = ref(false)
const linkUrl = ref('')
const linkText = ref('')
const linkNeedsText = ref(false)
const linkError = ref('')
const linkEditing = ref(false)

function openLink() {
  const e = editor.value
  if (!e || disabled.value)
    return
  const href = (e.getAttributes('link').href as string | undefined) ?? ''
  linkEditing.value = !!href
  linkUrl.value = href
  linkText.value = ''
  linkError.value = ''
  linkNeedsText.value = !href && e.state.selection.empty
  linkOpen.value = true
}

function applyLink() {
  const e = editor.value
  if (!e)
    return
  let url = linkUrl.value.trim()
  if (!url) {
    linkError.value = 'Add a web address or an email.'
    return
  }
  if (/^[^\s@/]+@[^\s@/]+\.[^\s@/]+$/.test(url))
    url = `mailto:${url}`
  else if (!/^[a-z][a-z\d+.-]*:/i.test(url))
    url = `https://${url}`
  if (!isSafeUrl(url) || /\s/.test(url)) {
    linkError.value = 'Use a web address (https://…) or an email.'
    return
  }
  const chain = e.chain().focus()
  if (linkNeedsText.value) {
    const text = linkText.value.trim() || url.replace(/^mailto:/i, '')
    chain.insertContent({ type: 'text', text, marks: [{ type: 'link', attrs: { href: url } }] }).run()
  }
  else {
    chain.extendMarkRange('link').setLink({ href: url }).run()
  }
  linkOpen.value = false
}

function removeLink() {
  editor.value?.chain().focus().extendMarkRange('link').unsetLink().run()
  linkOpen.value = false
}

function onContentKeydown(event: KeyboardEvent) {
  if ((event.metaKey || event.ctrlKey) && !event.shiftKey && !event.altKey && event.key.toLowerCase() === 'k') {
    event.preventDefault()
    openLink()
  }
}

// ---- modes ------------------------------------------------------------------------------------
function setMode(next: 'write' | 'markdown') {
  if (mode.value === next)
    return
  mode.value = next
  if (next === 'write')
    nextTick(() => editor.value?.commands.focus('end'))
}

const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
</script>

<template>
  <div
    class="g-mde"
    :class="{
      'g-mde--invalid': invalid,
      'g-mde--disabled': disabled,
      'g-mde--markdown': mode === 'markdown',
    }"
    v-bind="wrapperAttrs"
  >
    <div class="g-mde__bar">
      <div
        v-show="mode === 'write'"
        ref="toolbar"
        class="g-mde__tools"
        role="toolbar"
        aria-label="Formatting"
        @keydown="onToolbarKeydown"
      >
        <template
          v-for="(tool, index) in tools"
          :key="tool.key"
        >
          <span
            v-if="tool.group"
            class="g-mde__divider"
            aria-hidden="true"
          />
          <button
            type="button"
            class="g-mde__tool"
            :tabindex="index === focusIndex ? 0 : -1"
            :aria-label="tool.label"
            :title="tool.shortcut ? `${tool.label} (${tool.shortcut})` : tool.label"
            :aria-keyshortcuts="tool.shortcut ? tool.shortcut.replace('⌘', 'Meta+').replace('⇧', 'Shift+') : undefined"
            :aria-pressed="tool.isActive && editor ? tool.isActive(editor) : undefined"
            :disabled="isDisabled(tool)"
            @mousedown.prevent
            @click="focusIndex = index; runTool(tool)"
          >
            <Icon
              :name="tool.icon"
              aria-hidden="true"
            />
          </button>
        </template>
      </div>
      <p
        v-if="mode === 'markdown'"
        class="g-mde__hint"
      >
        Writing Markdown: **bold**, *italic*, ## heading, - list, [text](https://…)
      </p>

      <div
        class="g-mde__modes"
        role="group"
        aria-label="Editor view"
      >
        <button
          type="button"
          class="g-mde__mode"
          :aria-pressed="mode === 'write'"
          :disabled="disabled"
          @click="setMode('write')"
        >
          Write
        </button>
        <button
          type="button"
          class="g-mde__mode"
          :aria-pressed="mode === 'markdown'"
          :disabled="disabled"
          @click="setMode('markdown')"
        >
          Markdown
        </button>
      </div>
    </div>

    <EditorContent
      v-show="mode === 'write'"
      :editor="editor"
      class="g-mde__body"
      @keydown="onContentKeydown"
    />
    <div
      v-if="mode === 'markdown'"
      class="g-mde__source"
    >
      <GTextarea
        :model-value="model ?? ''"
        :rows="minRows"
        autoresize
        :max-rows="40"
        spellcheck="true"
        class="g-mde__textarea"
        @update:model-value="model = $event"
        :aria-label="attrs['aria-label'] ? `${attrs['aria-label']} (Markdown)` : undefined"
      />
    </div>

    <GModal
      v-model:open="linkOpen"
      :title="linkEditing ? 'Edit link' : 'Add link'"
      size="sm"
    >
      <form
        id="g-mde-link-form"
        class="g-mde__link-form"
        novalidate
        @submit.prevent="applyLink"
      >
        <GFormField
          label="Web address or email"
          :error="linkError"
        >
          <GInput
            v-model="linkUrl"
            type="url"
            inputmode="url"
            placeholder="https://geekshacking.com"
            autocomplete="url"
            spellcheck="false"
            autocapitalize="off"
            autofocus
            @update:model-value="linkError = ''"
          />
        </GFormField>
        <GFormField
          v-if="linkNeedsText"
          label="Text to show"
          hint="Optional. Uses the address if left empty."
        >
          <GInput v-model="linkText" />
        </GFormField>
      </form>
      <template #footer>
        <GButton
          v-if="linkEditing"
          variant="ghost"
          icon="material-symbols:link-off-rounded"
          label="Remove link"
          class="mr-auto"
          @click="removeLink"
        />
        <GButton
          variant="secondary"
          label="Cancel"
          @click="linkOpen = false"
        />
        <GButton
          variant="primary"
          type="submit"
          form="g-mde-link-form"
          :label="linkEditing ? 'Save link' : 'Add link'"
        />
      </template>
    </GModal>
  </div>
</template>

<style>
.g-mde {
  display: flex;
  flex-direction: column;
  width: 100%;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: var(--gh-text);
  font-family: var(--font-body);
  font-size: 15px;
  letter-spacing: var(--gh-tracking);
  transition:
    box-shadow var(--gh-dur-fast) var(--ease-gh),
    border-color var(--gh-dur-fast) var(--ease-gh);
}
.g-mde:has(.g-mde__content:focus) {
  border-color: var(--gh-field-focus-border);
  box-shadow: var(--gh-field-focus);
}
.g-mde--invalid,
.g-mde--invalid:has(.g-mde__content:focus) {
  border-color: var(--gh-err);
  box-shadow: var(--gh-field-error);
}
.g-mde--disabled {
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-disabled);
}

/* toolbar row */
.g-mde__bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 12px;
  padding: 6px;
  border-bottom: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: calc(var(--radius-gh) - 2px) calc(var(--radius-gh) - 2px) 0 0;
  background: var(--gh-surface-sunken);
}
.g-mde__tools {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 2px;
}
.g-mde__divider {
  width: 1px;
  height: 20px;
  margin: 0 4px;
  background: var(--gh-line-soft);
}
.g-mde__tool {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  padding: 0;
  border: var(--gh-border-width) solid transparent;
  border-radius: var(--radius-gh-sm);
  background: none;
  color: var(--gh-text);
  cursor: pointer;
}
.g-mde__tool > * {
  width: 20px;
  height: 20px;
}
.g-mde__tool:hover:not(:disabled) {
  background: var(--gh-ghost-hover);
}
/* a format that's on: the brand's yellow block */
.g-mde__tool[aria-pressed='true'] {
  border-color: var(--gh-on-accent);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.g-mde__tool:disabled {
  color: var(--gh-text-disabled);
  cursor: not-allowed;
}
.g-mde__tool:focus-visible,
.g-mde__mode:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.g-mde__hint {
  flex: 1 1 14rem;
  margin: 0;
  padding: 0 6px;
  font-size: 13px;
  color: var(--gh-text-muted);
  font-family: var(--font-mono);
}

/* Write / Markdown switch */
.g-mde__modes {
  display: inline-flex;
  margin-left: auto;
  padding: 2px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface);
}
.g-mde__mode {
  padding: 4px 10px;
  border: var(--gh-border-width) solid transparent;
  border-radius: calc(var(--radius-gh-sm) - 2px);
  background: none;
  color: var(--gh-text-muted);
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
}
.g-mde__mode:hover:not(:disabled) {
  color: var(--gh-text);
}
.g-mde__mode[aria-pressed='true'] {
  border-color: var(--gh-line);
  background: var(--gh-surface-raised);
  color: var(--gh-text);
}
.g-mde__mode:disabled {
  cursor: not-allowed;
}

/* writing area */
.g-mde__content {
  padding: 12px 14px;
  outline: none;
  white-space: pre-wrap;
}
.g-mde__content p.is-editor-empty:first-child::before {
  content: attr(data-placeholder);
  float: left;
  height: 0;
  color: var(--gh-text-subtle);
  pointer-events: none;
}
.g-mde--disabled .g-mde__content {
  cursor: not-allowed;
}
.g-mde__content a {
  cursor: text;
}

/* link dialog (teleported, so these styles are unscoped) */
.g-mde__link-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* raw Markdown: the textarea sits flush inside the box */
.g-mde__source {
  padding: 0;
}
.g-mde .g-mde__textarea.g-textarea,
.g-mde .g-mde__textarea.g-textarea:focus-within {
  border: 0;
  border-radius: 0 0 calc(var(--radius-gh) - 2px) calc(var(--radius-gh) - 2px);
  background: transparent;
  box-shadow: none;
  font-family: var(--font-mono);
  font-size: 14px;
}
.g-mde:has(.g-mde__textarea:focus) {
  border-color: var(--gh-field-focus-border);
  box-shadow: var(--gh-field-focus);
}
</style>
