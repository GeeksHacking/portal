<script setup lang="ts">
import type { HeaderImageChange } from '~/composables/useActivityHeaderImage'
import { HEADER_IMAGE_ACCEPT, HEADER_IMAGE_MAX_BYTES } from '~/composables/useActivityHeaderImage'

/**
 * Header image picker for activities. It only records what the admin wants to change;
 * the parent saves it (see useActivityHeaderImage) after the activity itself is saved.
 */
const props = defineProps<{
  /** Image currently stored on the activity, if any. */
  currentUrl?: string | null
}>()

const change = defineModel<HeaderImageChange>({ required: true })

const input = ref<HTMLInputElement>()
const error = ref<string>()
const previewUrl = ref<string | null>(null)

watch(
  () => change.value.file,
  (file, _old, onCleanup) => {
    if (!file) {
      previewUrl.value = null
      return
    }
    const url = URL.createObjectURL(file)
    previewUrl.value = url
    onCleanup(() => URL.revokeObjectURL(url))
  },
  { immediate: true },
)

const shownUrl = computed(() =>
  previewUrl.value ?? (change.value.removed ? null : props.currentUrl ?? null),
)

function onPick(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  target.value = ''
  if (!file)
    return

  if (!HEADER_IMAGE_ACCEPT.includes(file.type)) {
    error.value = 'Use a JPEG, PNG or WebP image.'
    return
  }
  if (file.size > HEADER_IMAGE_MAX_BYTES) {
    error.value = 'Image must be 5 MB or smaller.'
    return
  }

  error.value = undefined
  change.value = { file, removed: false }
}

function remove() {
  error.value = undefined
  change.value = { file: null, removed: !!props.currentUrl }
}
</script>

<template>
  <UFormField
    label="Header image"
    description="Shown at the top of the event page and on event cards. JPEG, PNG or WebP, up to 5 MB. Use a 2:1 image, e.g. 2160 × 1080."
    :error="error"
  >
    <div class="space-y-2">
      <div class="aspect-2/1 w-full overflow-hidden rounded-md border border-default bg-elevated">
        <img
          v-if="shownUrl"
          :src="shownUrl"
          alt="Header image preview"
          class="size-full object-cover"
        >
        <div
          v-else
          class="flex size-full items-center justify-center text-muted"
        >
          <UIcon
            name="i-lucide-image"
            class="size-8"
          />
        </div>
      </div>

      <div class="flex flex-wrap gap-2">
        <UButton
          size="sm"
          variant="outline"
          color="neutral"
          icon="i-lucide-upload"
          @click="input?.click()"
        >
          {{ shownUrl ? 'Replace image' : 'Choose image' }}
        </UButton>
        <UButton
          v-if="shownUrl"
          size="sm"
          variant="ghost"
          color="error"
          icon="i-lucide-trash-2"
          @click="remove"
        >
          Remove
        </UButton>
      </div>

      <input
        ref="input"
        type="file"
        class="hidden"
        :accept="HEADER_IMAGE_ACCEPT.join(',')"
        @change="onPick"
      >
    </div>
  </UFormField>
</template>
