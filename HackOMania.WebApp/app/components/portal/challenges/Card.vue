<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

const props = defineProps<{
  title: string
  teamCount: number
  selected: boolean
  titleHeight?: number
  colours?: string[] | null
}>()

const emit = defineEmits<{
  select: []
  titleMounted: [height: number]
}>()

const titleRef = ref<HTMLElement | null>(null)

// Compute the header style with gradient and brightness
const headerStyle = computed(() => {
  const style: Record<string, string> = {}

  if (props.titleHeight) {
    style.height = `${props.titleHeight}px`
  }

  if (props.colours) {
    style.background = `linear-gradient(to right, ${props.colours[0]} 0%, ${props.colours[1]} 94%, ${props.colours[2]} 100%)`
    style.opacity = props.selected ? '1' : '0.4'
  }

  return style
})

// Fallback class when no custom colours
const headerClass = computed(() => {
  if (props.colours)
    return ''
  return props.selected ? 'bg-[#FF5B84]' : 'bg-[#FF5B84]/40'
})

onMounted(() => {
  if (titleRef.value) {
    emit('titleMounted', titleRef.value.offsetHeight)
  }
})

watch(() => props.title, () => {
  setTimeout(() => {
    if (titleRef.value) {
      emit('titleMounted', titleRef.value.offsetHeight)
    }
  }, 0)
})
</script>

<template>
  <div
    class="shadow-sm rounded-lg overflow-hidden cursor-pointer"
    @click="emit('select')"
  >
    <div
      ref="titleRef"
      class="px-3 lg:px-4 py-3 lg:py-4 rounded-lg text-center min-h-12 lg:min-h-18 flex items-center justify-center"
      :class="headerClass"
      :style="headerStyle"
    >
      <span class="font-['Zalando_Sans_Expanded'] text-black uppercase text-lg lg:text-xl break-words">
        {{ title }}
      </span>
    </div>
    <div class="bg-white py-6 lg:py-8 px-4 lg:px-6 text-center flex flex-col gap-4 lg:gap-4">
      <div class="font-['Zalando_Sans_Expanded'] font-bold text-4xl lg:text-5xl">
        {{ teamCount }}
      </div>
      <div class="font-['Zalando_Sans_Expanded'] text-xl lg:text-2xl">
        Teams
      </div>
    </div>
  </div>
</template>
