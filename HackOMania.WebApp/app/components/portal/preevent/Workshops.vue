<script setup lang="ts">
import type { WorkshopItem } from '~/data/preevent'

defineProps<{
  items: WorkshopItem[]
}>()
</script>

<template>
  <div>
    <h3 class="font-['Zalando_Sans_Expanded'] font-bold uppercase text-3xl mb-8">Workshops</h3>
    <div class="flex flex-col gap-8 lg:gap-12">
      <PortalPreeventWorkshopCard
        v-for="(item, index) in items"
        :key="index"
        :logo="item.logo"
        :logo-class="item.logoClass"
        :title="item.title"
        :description="item.description"
        :speaker-name="item.speakerName"
        :speaker-background="item.speakerBackground"
      />
    </div>
  </div>
</template>
