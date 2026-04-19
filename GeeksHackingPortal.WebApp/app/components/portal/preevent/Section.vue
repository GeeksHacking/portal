<script setup lang="ts">
import { itineraryItems, workshopItems } from '~/data/preevent'
</script>

<template>
  <section>
    <header
      class="h-18 flex items-center justify-center bg-[linear-gradient(to_right,#A1E3FB_0%,#74D9FF_75%,#1FC3FF_100%)] border-y border-y-black"
      style="border-image: linear-gradient(to right, black, transparent) 1"
    >
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        PRE-EVENT
      </h2>
    </header>
    <div class="flex flex-col lg:flex-row gap-8 lg:gap-12 p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300">
      <div class="w-full lg:w-2/5">
        <PortalPreeventItinerary :items="itineraryItems" />
      </div>
      <div class="w-full lg:w-3/5">
        <PortalPreeventWorkshops :items="workshopItems" />
      </div>
    </div>
  </section>
</template>
