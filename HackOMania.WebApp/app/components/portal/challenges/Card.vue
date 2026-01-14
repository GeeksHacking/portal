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
      class="px-4 rounded-lg text-center h-18 flex items-center justify-center"
      :class="selected ? 'bg-[#FF5B84]' : 'bg-[#FF5B84]/40'"
    >
      <span class="font-['Zalando_Sans_Expanded'] text-black uppercase text-2xl">
        {{ title }}
      </span>
    </div>
    <div class="bg-white py-12 px-6 text-center flex flex-col gap-8">
      <div class="font-['Zalando_Sans_Expanded'] font-bold text-7xl">
        {{ teamCount }}
      </div>
      <div class="font-['Zalando_Sans_Expanded'] text-4xl">
        Teams
      </div>
    </div>
  </div>
</template>
