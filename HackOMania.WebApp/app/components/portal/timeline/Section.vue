<script setup lang="ts">
import { prehackItems, day1Items, day2Items } from '~/data/timeline'
</script>

<template>
  <section>
    <header
      class="h-18 flex items-center justify-center bg-[linear-gradient(to_right,#A1E3FB_0%,#74D9FF_75%,#1FC3FF_100%)] border-y border-y-black"
      style="border-image: linear-gradient(to right, black, transparent) 1"
    >
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        TIMELINE
      </h2>
    </header>
    <div class="mx-auto max-w-7xl p-8 lg:py-16 lg:px-28">
      <div class="flex flex-col lg:flex-row gap-8 lg:gap-12">
        <div class="w-full lg:w-1/3">
          <PortalPreeventItinerary title="PREHACK" :items="prehackItems" />
        </div>
        <div class="w-full lg:w-1/3">
          <PortalPreeventItinerary title="DAY 1" :items="day1Items" />
        </div>
        <div class="w-full lg:w-1/3">
          <PortalPreeventItinerary title="DAY 2" :items="day2Items" />
        </div>
      </div>
    </div>
  </section>
</template>
