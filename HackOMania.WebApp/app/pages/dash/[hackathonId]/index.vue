<script setup lang="ts">
import { computed, watch } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { organizerQueries } from '~/composables/organizers'
import { authQueries } from '~/composables/auth'
import Participants from './participants.vue'
import Challenges from './challenges.vue'
import Teams from './teams.vue'
import Submissions from './submissions.vue'
import Judges from './judges.vue'

const route = useRoute()

const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)

// Fetch current user
const { data: user, isLoading: isLoadingUser } = useQuery(authQueries.whoAmI)

// Fetch organizers list
const { data: organizersData, isLoading: isLoadingOrganizers } = useQuery(
  computed(() => ({
    ...organizerQueries.list(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

// Check if current user is an organizer
const isOrganizer = computed(() => {
  if (!user.value?.id) {
    return false
  }
  if (user.value.isRoot)
    return true
  if (organizersData.value?.organizers) {
    return organizersData.value.organizers.some(org => org.userId === user.value?.id)
  }
  return false
})

const isLoadingOrganizerCheck = computed(() => isLoadingUser.value || isLoadingOrganizers.value)

// Redirect to participant view if not an organizer
watch([isOrganizer, isLoadingOrganizerCheck], ([org, loading]) => {
  if (!loading && !org) {
    navigateTo(`/dash/${hackathonId.value}/participant`)
  }
})

const { data: hackathon, isLoading: isLoadingHackathon } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)
</script>

<template>
  <UDashboardPanel id="hackathon-organizer">
    <template #header>
      <UDashboardNavbar :title="hackathon?.name ?? 'Hackathon'">
        <template #leading>
          <UButton
            to="/dash"
            icon="i-lucide-arrow-left"
            color="neutral"
            variant="ghost"
            size="sm"
          >
            Back
          </UButton>
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 space-y-4">
        <div
          v-if="isLoadingOrganizerCheck || isLoadingHackathon"
          class="text-(--ui-text-muted)"
        >
          Loading...
        </div>

        <template v-else-if="isOrganizer">
          <div class="flex flex-col gap-1">
            <h2 class="text-lg font-semibold">
              Organizer Dashboard
            </h2>
            <p class="text-sm text-(--ui-text-muted)">
              Manage participants, teams, and submissions for {{ hackathon?.name }}.
            </p>
          </div>

          <Participants
            :hackathon-id="hackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Teams
            :hackathon-id="hackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Challenges
            :hackathon-id="hackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Judges
            :hackathon-id="hackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Submissions
            :hackathon-id="hackathonId ?? ''"
            :is-organizer="isOrganizer"
          />
        </template>
      </div>
    </template>
  </UDashboardPanel>
</template>
