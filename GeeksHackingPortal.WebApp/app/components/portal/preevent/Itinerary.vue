<script setup lang="ts">
import type { ItineraryItem } from '~/data/preevent'

withDefaults(defineProps<{
  items: ItineraryItem[]
  title?: string
}>(), {
  title: 'Itinerary',
})
</script>

<template>
  <div>
    <h3 class="font-['Zalando_Sans_Expanded'] font-bold uppercase text-3xl mb-8">
      {{ title }}
    </h3>
    <div class="flex flex-col gap-6">
      <PortalPreeventItineraryCard
        v-for="(item, index) in items"
        :key="index"
        :time="item.time"
        :event="item.event"
        :bg-color="item.bgColor"
      />
    </div>
  </div>
</template>
