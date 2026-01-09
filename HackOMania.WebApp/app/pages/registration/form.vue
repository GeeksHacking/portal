<script setup lang="ts">
import FormHeader from '~/components/registration/FormHeader.vue'
import AccountDetails from '~/components/registration/AccountDetails.vue'
import SkillsAndLogistics from '~/components/registration/SkillsAndLogistics.vue'
import Participation from '~/components/registration/Participation.vue'

const route = useRoute()

// Get current index from query params, default to 0
const currentIndex = computed(() => {
  const index = route.query.index
  return index ? Number(index) : 0
})

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})
</script>

<template>
  <div class="bg-white min-h-screen font-raleway">
    <FormHeader />

    <!-- Dynamic content based on current step -->
    <div>
      <AccountDetails v-if="currentIndex === 0" />
      <SkillsAndLogistics v-if="currentIndex === 1" />
      <Participation v-if="currentIndex === 2" />
    </div>
  </div>
</template>
