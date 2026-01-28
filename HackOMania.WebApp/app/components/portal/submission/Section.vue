<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { ref, computed, watch } from 'vue'

const hackathonId = useRouteHackathonId()
const hackathon = useRouteHackathon()
const toast = useToast()

// Fetch participation status
const { data: statusData } = useQuery(
  computed(() => ({
    ...hackathonQueries.status(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

// Fetch current user's team
const { data: teamData, isLoading: isLoadingTeam } = useQuery(
  computed(() => ({
    ...teamQueries.me(hackathonId.value ?? ''),
    enabled: !!hackathonId.value && !!statusData.value?.isParticipant,
  })),
)

const isParticipant = computed(() => !!statusData.value?.isParticipant)
const hasTeam = computed(() => !!teamData.value?.id)

const teamId = computed(() => teamData.value?.id ?? null)
const challengeId = computed(() => teamData.value?.challengeId ?? null)
const submissionsStartDate = computed(() => hackathon.value?.submissionsStartDate ?? null)
const submissionsEndDate = computed(() => hackathon.value?.submissionsEndDate ?? null)

// Computed submission status based on dates
const submissionStatus = computed(() => {
  const now = new Date()
  if (!submissionsStartDate.value) return 'upcoming'
  if (now < submissionsStartDate.value) return 'upcoming'
  if (submissionsEndDate.value && now > submissionsEndDate.value) return 'closed'
  return 'open'
})

// Fetch existing submissions for the team
const { data: submissionsData } = useQuery(
  computed(() => ({
    ...submissionQueries.listForTeam(hackathonId.value ?? '', teamId.value ?? ''),
    enabled: !!hackathonId.value && !!teamId.value,
  })),
)

// Check if team has any existing submissions
const hasExistingSubmissions = computed(() => {
  return (submissionsData.value?.submissions?.length ?? 0) > 0
})

// Get the existing submission (for displaying submitted values)
const existingSubmission = computed(() => {
  return submissionsData.value?.submissions?.[0] ?? null
})

// Form disabled when not open OR already submitted
const isFormDisabled = computed(() =>
  submissionStatus.value !== 'open' || hasExistingSubmissions.value,
)

// Confirmation modal state
const showConfirmModal = ref(false)

// Form fields
const title = ref('')
const summary = ref('')
const repoUri = ref('')
const demoUri = ref('')
const slidesUri = ref('')
const devpostUri = ref('')
const location = ref('')

// URL validation
function isValidUrl(url: string): boolean {
  if (!url.trim()) return true // Empty is valid (optional field)
  try {
    new URL(url)
    return true
  } catch {
    return false
  }
}

// Populate form from existing submission (for display when already submitted)
watch(existingSubmission, (submission) => {
  if (submission) {
    title.value = submission.title ?? ''
    summary.value = submission.summary ?? ''
    repoUri.value = submission.repoUri ?? ''
    demoUri.value = submission.demoUri ?? ''
    slidesUri.value = submission.slidesUri ?? ''
    devpostUri.value = submission.devpostUri ?? ''
    location.value = submission.location ?? ''
  }
}, { immediate: true })

// Mutation to create submission
const createSubmissionMutation = useCreateSubmission(hackathonId, teamId)

const isSubmitting = computed(() => createSubmissionMutation.isPending.value)

// Validates form and opens confirmation modal
function openConfirmModal() {
  if (!title.value.trim()) {
    toast.add({
      title: 'Title is required',
      description: 'Please enter a title for your submission.',
      color: 'error',
    })
    return
  }

  if (!challengeId.value) {
    toast.add({
      title: 'No challenge selected',
      description: 'Please select a challenge before submitting.',
      color: 'error',
    })
    return
  }

  // Validate URLs
  const urlFields = [
    { name: 'Repository URL', value: repoUri.value },
    { name: 'Demo URL', value: demoUri.value },
    { name: 'Slides URL', value: slidesUri.value },
    { name: 'Devpost URL', value: devpostUri.value },
  ]

  for (const field of urlFields) {
    if (!isValidUrl(field.value)) {
      toast.add({
        title: 'Invalid URL',
        description: `${field.name} is not a valid URL.`,
        color: 'error',
      })
      return
    }
  }

  // All validation passed, show confirmation modal
  showConfirmModal.value = true
}

// Performs actual submission
function confirmSubmit() {
  if (!challengeId.value) return

  createSubmissionMutation.mutate({
    challengeId: challengeId.value,
    title: title.value.trim(),
    summary: summary.value.trim() || null,
    repoUri: repoUri.value.trim() || null,
    demoUri: demoUri.value.trim() || null,
    slidesUri: slidesUri.value.trim() || null,
    devpostUri: devpostUri.value.trim() || null,
    location: location.value.trim() || null,
  }, {
    onSuccess() {
      showConfirmModal.value = false
      toast.add({
        title: 'Submission successful',
        description: 'Your submission has been received successfully.',
        color: 'success',
      })
    },
    onError() {
      toast.add({
        title: 'Failed to submit',
        description: 'Please try again.',
        color: 'error',
      })
    },
  })
}
</script>

<template>
  <section class="w-full">
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-linear-to-r from-[#4B8BF5] via-[#7DB4FF] to-[#AAD4FF]">
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        SUBMISSION
      </h2>
    </header>

    <!-- Loading state -->
    <div
      v-if="isLoadingTeam"
      class="p-8 lg:py-16 lg:px-28"
    >
      Loading...
    </div>

    <!-- Not a participant or no team -->
    <div
      v-else-if="!isParticipant || !hasTeam"
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <p class="font-['Raleway'] text-base lg:text-xl">
        You need to be part of a team to submit a project.
      </p>
    </div>

    <!-- Has team -->
    <div
      v-else
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <div class="w-full text-black text-left">
        <!-- Already submitted message -->
        <div
          v-if="hasExistingSubmissions"
          class="mt-6 p-4 bg-green-50 border border-green-200 rounded-lg"
        >
          <p class="font-['Raleway'] text-base lg:text-xl text-green-800">
            Your team has already submitted a project.
          </p>
        </div>

        <!-- Status messages (only show if not already submitted) -->
        <template v-if="!hasExistingSubmissions">
          <div
            v-if="submissionStatus === 'upcoming'"
            class="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg"
          >
            <p class="font-['Raleway'] text-base lg:text-xl text-yellow-800">
              Submission will open when the hackathon starts.
            </p>
          </div>

          <div
            v-else-if="submissionStatus === 'closed'"
            class="mt-6 p-4 bg-gray-50 border border-gray-200 rounded-lg"
          >
            <p class="font-['Raleway'] text-base lg:text-xl text-gray-600">
              Submission period has closed.
            </p>
          </div>

          <!-- No challenge selected warning -->
          <div
            v-if="!challengeId"
            class="mt-6 p-4 bg-orange-50 border border-orange-200 rounded-lg"
          >
            <p class="font-['Raleway'] text-base lg:text-xl text-orange-800">
              Please select a challenge statement in the Team section above before submitting.
            </p>
          </div>
        </template>

        <!-- Submission form -->
        <div class="mt-8 space-y-6">
          <!-- Title -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Project Title *
            </label>
            <input
              v-model="title"
              type="text"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="Enter your project title"
              :disabled="isFormDisabled"
            >
          </div>

          <!-- Summary -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Summary
            </label>
            <textarea
              v-model="summary"
              rows="3"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="Describe your project briefly..."
              :disabled="isFormDisabled"
            />
          </div>

          <!-- Repository URL -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Repository URL
            </label>
            <input
              v-model="repoUri"
              type="url"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="https://github.com/your-repo"
              :disabled="isFormDisabled"
            >
          </div>

          <!-- Demo URL -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Demo URL
            </label>
            <input
              v-model="demoUri"
              type="url"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="https://your-demo-link.com"
              :disabled="isFormDisabled"
            >
          </div>

          <!-- Slides URL -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Slides URL
            </label>
            <input
              v-model="slidesUri"
              type="url"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="https://slides.google.com/..."
              :disabled="isFormDisabled"
            >
          </div>

          <!-- Devpost URL -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Devpost URL
            </label>
            <input
              v-model="devpostUri"
              type="url"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="https://devpost.com/software/..."
              :disabled="isFormDisabled"
            >
          </div>

          <!-- Location -->
          <div>
            <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
              Location
            </label>
            <input
              v-model="location"
              type="text"
              class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
              placeholder="e.g., Table 5, Room A"
              :disabled="isFormDisabled"
            >
          </div>

          <!-- Submit Button (only show if not already submitted) -->
          <div
            v-if="!hasExistingSubmissions"
            class="flex gap-4 mt-8"
          >
            <button
              class="flex-1 py-3 bg-linear-to-r from-[#4B8BF5] via-[#7DB4FF] to-[#AAD4FF] text-black rounded font-['Zalando_Sans_Expanded'] text-base lg:text-xl hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="isFormDisabled || isSubmitting || !challengeId"
              @click="openConfirmModal"
            >
              Submit
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirmation Modal -->
    <UModal v-model:open="showConfirmModal">
      <template #content>
        <div class="p-6">
          <h3 class="font-['Zalando_Sans_Expanded'] text-lg font-bold mb-2">
            Confirm Submission
          </h3>
          <p class="font-['Raleway'] text-base mb-4 text-gray-600">
            Please review your submission details carefully. This cannot be changed after submission.
          </p>

          <!-- Submission Details -->
          <div class="space-y-3 mb-6 font-['Raleway'] text-sm">
            <div>
              <span class="font-bold">Project Title:</span> {{ title }}
            </div>
            <div v-if="summary">
              <span class="font-bold">Summary:</span> {{ summary }}
            </div>
            <div v-if="repoUri">
              <span class="font-bold">Repository:</span> {{ repoUri }}
            </div>
            <div v-if="demoUri">
              <span class="font-bold">Demo:</span> {{ demoUri }}
            </div>
            <div v-if="slidesUri">
              <span class="font-bold">Slides:</span> {{ slidesUri }}
            </div>
            <div v-if="devpostUri">
              <span class="font-bold">Devpost:</span> {{ devpostUri }}
            </div>
            <div v-if="location">
              <span class="font-bold">Location:</span> {{ location }}
            </div>
          </div>

          <div class="flex gap-3 justify-end font-['Raleway']">
            <UButton
              color="neutral"
              variant="outline"
              @click="showConfirmModal = false"
            >
              Cancel
            </UButton>
            <UButton
              :loading="isSubmitting"
              @click="confirmSubmit"
            >
              Confirm Submission
            </UButton>
          </div>
        </div>
      </template>
    </UModal>
  </section>
</template>
