<script setup lang="ts">
defineProps<{
  title: string
  teamCount: number
  selected: boolean
}>()

const emit = defineEmits<{
  select: []
}>()
</script>

<template>
  <div
    class="shadow-sm rounded-lg overflow-hidden cursor-pointer"
    @click="emit('select')"
  >
    <div
      class="px-3 lg:px-4 rounded-lg text-center h-12 lg:h-18 flex items-center justify-center"
      :class="selected ? 'bg-[#FF5B84]' : 'bg-[#FF5B84]/40'"
    >
      <span class="font-['Zalando_Sans_Expanded'] text-black uppercase text-base lg:text-2xl">
        {{ title }}
      </span>
    </div>
    <div class="bg-white py-6 lg:py-12 px-4 lg:px-6 text-center flex flex-col gap-4 lg:gap-8">
      <div class="font-['Zalando_Sans_Expanded'] font-bold text-4xl lg:text-7xl">
        {{ teamCount }}
      </div>
      <div class="font-['Zalando_Sans_Expanded'] text-xl lg:text-4xl">
        Teams
      </div>
    </div>
  </div>
</template>
