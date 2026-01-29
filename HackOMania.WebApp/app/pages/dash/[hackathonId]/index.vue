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
import Questions from './questions.vue'

const route = useRoute()

const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? null)

// Fetch hackathon first to get the actual ID
const { data: hackathon, isLoading: isLoadingHackathon } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonIdOrShortCode.value ?? ''),
    enabled: !!hackathonIdOrShortCode.value,
  })),
)

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)

// Fetch current user
const { data: user, isLoading: isLoadingUser } = useQuery(authQueries.whoAmI)

// Fetch organizers list
const { data: organizersData, isLoading: isLoadingOrganizers } = useQuery(
  computed(() => ({
    ...organizerQueries.list(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
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
    navigateTo(`/dash/${hackathonIdOrShortCode.value}/participant`)
  }
})
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
      <div class="p-4 space-y-4 overflow-y-auto">
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
            :hackathon-id="resolvedHackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Teams
            :hackathon-id="resolvedHackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Challenges
            :hackathon-id="resolvedHackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Judges
            :hackathon-id="resolvedHackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Submissions
            :hackathon-id="resolvedHackathonId ?? ''"
            :is-organizer="isOrganizer"
          />

          <Questions
            :hackathon-id="resolvedHackathonId ?? ''"
            :is-organizer="isOrganizer"
          />
        </template>
      </div>
    </template>
  </UDashboardPanel>
</template>
