<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed, watch } from 'vue'
import { authQueries } from '~/composables/auth'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { organizerQueries } from '~/composables/organizers'

const route = useRoute()
const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? null)

const { data: hackathon } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonIdOrShortCode.value ?? ''),
    enabled: !!hackathonIdOrShortCode.value,
  })),
)

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)
const { data: user, isLoading: isLoadingUser } = useQuery(authQueries.whoAmI)
const { data: organizersData, isLoading: isLoadingOrganizers } = useQuery(
  computed(() => ({
    ...organizerQueries.list(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
  })),
)

const isOrganizer = computed(() => {
  if (!user.value?.id)
    return false
  if (user.value.isRoot)
    return true
  return organizersData.value?.organizers?.some(org => org.userId === user.value?.id) ?? false
})

watch([isOrganizer, isLoadingUser, isLoadingOrganizers], ([organizer, loadingUser, loadingOrganizers]) => {
  if (loadingUser || loadingOrganizers || !hackathonIdOrShortCode.value)
    return

  if (organizer) {
    navigateTo(`/dash/${hackathonIdOrShortCode.value}/checkin`)
    return
  }

  navigateTo(`/dash/${hackathonIdOrShortCode.value}/participant`)
}, { immediate: true })
</script>

<template>
  <div class="p-4 text-sm text-(--ui-text-muted)">
    Redirecting...
  </div>
</template>
