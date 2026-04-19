<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed, ref, watch } from 'vue'

const props = defineProps<{
  teamName: string
  teamId: string
  hackathonId: string
  selectedChallengeId: string | null
}>()

const toast = useToast()

const hackathonIdRef = computed(() => props.hackathonId)
const teamIdRef = computed(() => props.teamId)
const hackathon = useRouteHackathon()

// Fetch challenges list for the hackathon
const { data: challengesData } = useQuery(
  computed(() => ({
    ...challengeQueries.list(props.hackathonId),
    enabled: !!props.hackathonId,
  })),
)

const challenges = computed(() => [...(challengesData.value?.challenges ?? [])].reverse())

const challengeItems = computed(() =>
  challenges.value.map(challenge => ({
    label: challenge.title ?? '',
    value: challenge.id ?? '',
  })),
)
const challengeSelectionEndDate = computed(() => hackathon.value?.challengeSelectionEndDate ?? null)
const isChallengeSelectionClosed = computed(() => {
  if (!challengeSelectionEndDate.value)
    return false
  return new Date() > challengeSelectionEndDate.value
})

// Initialize from prop
const selectedChallenge = ref<string | undefined>(props.selectedChallengeId ?? undefined)

// Watch for prop changes
watch(() => props.selectedChallengeId, (newVal) => {
  selectedChallenge.value = newVal ?? undefined
})

// Mutation to persist challenge selection
const selectChallengeMutation = useSelectChallenge(hackathonIdRef, teamIdRef)

// Watch for selection changes and persist to API
watch(selectedChallenge, (newVal, oldVal) => {
  if (isChallengeSelectionClosed.value) {
    selectedChallenge.value = props.selectedChallengeId ?? undefined
    return
  }

  if (newVal && newVal !== oldVal && newVal !== props.selectedChallengeId) {
    selectChallengeMutation.mutate(newVal, {
      onSuccess() {
        const selectedItem = challengeItems.value.find(item => item.value === newVal)
        toast.add({
          title: 'Challenge updated',
          description: selectedItem?.label
            ? `Selected ${selectedItem.label}.`
            : 'Your team challenge statement has been updated.',
          color: 'success',
        })
      },
      onError() {
        toast.add({
          title: 'Failed to update challenge',
          description: 'Please try again.',
          color: 'error',
        })
        // Revert on error
        selectedChallenge.value = props.selectedChallengeId ?? undefined
      },
    })
  }
})
</script>

<template>
  <div class="w-full lg:max-w-3xl text-black">
    <h3 class="font-['Zalando_Sans_Expanded'] text-xl lg:text-3xl font-bold mb-2 uppercase">
      Challenge Statement
    </h3>

    <p class="font-['Raleway'] text-base lg:text-xl text-black/80">
      Select a challenge statement your team will be working on.
    </p>

    <p
      v-if="isChallengeSelectionClosed"
      class="font-['Raleway'] text-base lg:text-xl text-orange-700 mt-4"
    >
      Challenge selection is closed. Your current selection is locked in.
    </p>

    <p class="font-['Raleway'] text-base lg:text-xl text-black/80 mt-8.5">
      Selected Challenge Statement for:
    </p>
    <p class="font-['Zalando_Sans_Expanded'] text-2xl font-bold uppercase">
      {{ teamName }}
    </p>

    <USelect
      v-model="selectedChallenge"
      :items="challengeItems"
      value-key="value"
      label-key="label"
      placeholder="Choose a Challenge Statement"
      class="w-full mt-6"
      :disabled="isChallengeSelectionClosed"
      :ui="{
        base: 'bg-white rounded-lg border border-black font-[\'Raleway\'] text-base lg:text-2xl h-11 lg:h-14',
        content: 'font-[\'Raleway\'] text-base lg:text-2xl',
        item: 'font-[\'Raleway\'] text-base lg:text-2xl',
      }"
    />
  </div>
</template>
