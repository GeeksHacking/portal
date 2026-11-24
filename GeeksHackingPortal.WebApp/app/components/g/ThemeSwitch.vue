<script setup lang="ts">
/**
 * GThemeSwitch: Light / Dark / System, remembered per browser (Nuxt color-mode, which puts
 * `.dark` on <html>; the tokens follow it).
 *
 * - Default: a three-way segmented control built on native radios, so Tab lands on the chosen
 *   option and the arrow keys move and select (Reka's RadioGroup left every option at
 *   tabindex -1 until clicked).
 * - `compact`: one icon button that cycles Light → Dark → System, for the collapsed sidebar rail.
 */
withDefaults(defineProps<{
  compact?: boolean
}>(), {
  compact: false,
})

type Preference = 'light' | 'dark' | 'system'
const options: { value: Preference, label: string, icon: string }[] = [
  { value: 'light', label: 'Light', icon: 'material-symbols:light-mode-rounded' },
  { value: 'dark', label: 'Dark', icon: 'material-symbols:dark-mode-rounded' },
  { value: 'system', label: 'System', icon: 'material-symbols:computer-rounded' },
]

const colorMode = useColorMode()
const preference = computed<Preference>({
  get: () => (['light', 'dark', 'system'].includes(colorMode.preference) ? colorMode.preference as Preference : 'system'),
  set: (value) => {
    colorMode.preference = value
  },
})
const name = `g-theme-${useId()}`
const current = computed(() => options.find(o => o.value === preference.value) ?? options[2]!)

function cycle() {
  const index = options.findIndex(o => o.value === preference.value)
  preference.value = options[(index + 1) % options.length]!.value
}
</script>

<template>
  <GButton
    v-if="compact"
    variant="ghost"
    size="sm"
    :icon="current.icon"
    :aria-label="`Theme: ${current.label}. Change theme`"
    :title="`Theme: ${current.label}`"
    @click="cycle"
  />
  <fieldset
    v-else
    class="g-theme"
  >
    <legend class="sr-only">
      Theme
    </legend>
    <label
      v-for="option in options"
      :key="option.value"
      class="g-theme__item"
      :title="option.label"
    >
      <input
        v-model="preference"
        type="radio"
        :name="name"
        :value="option.value"
        class="g-theme__input"
      >
      <Icon
        :name="option.icon"
        class="g-theme__icon"
        aria-hidden="true"
      />
      <span class="sr-only">{{ option.label }}</span>
    </label>
  </fieldset>
</template>

<style scoped>
.g-theme {
  display: grid;
  min-width: 0;
  margin: 0;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 2px;
  padding: 2px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-sm);
}
.g-theme__item {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 32px;
  border: var(--gh-border-width) solid transparent;
  border-radius: 4px;
  background: transparent;
  color: var(--gh-text);
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.g-theme__item:hover {
  background: var(--gh-ghost-hover);
}
/* the radio itself is invisible but stays focusable; the label shows its state */
.g-theme__input {
  position: absolute;
  width: 1px;
  height: 1px;
  opacity: 0;
  pointer-events: none;
}
.g-theme__item:has(.g-theme__input:focus-visible) {
  outline: 3px solid var(--gh-focus);
  outline-offset: 1px;
}
/* chosen option: the same yellow block as the current nav item */
.g-theme__item:has(.g-theme__input:checked) {
  border-color: var(--gh-on-accent);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.g-theme__icon {
  width: 18px;
  height: 18px;
}
@media (prefers-reduced-motion: reduce) {
  .g-theme__item { transition: none; }
}
</style>
