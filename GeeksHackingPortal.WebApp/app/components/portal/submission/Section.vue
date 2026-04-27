<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQuery } from '@tanstack/vue-query'
import { computed, ref } from 'vue'

const hackathonId = useResolvedHackathonId()
const hackathon = useRouteHackathon()
const toast = useToast()

// Fetch participation status
const { data: statusData } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint(
  computed(() => hackathonId.value ?? ''),
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
const eventStartDate = computed(() => hackathon.value?.eventStartDate ?? null)
const submissionsStartDate = computed(() => hackathon.value?.submissionsStartDate ?? null)
const submissionsEndDate = computed(() => hackathon.value?.submissionsEndDate ?? null)

// Check if hackathon has started
const hasHackathonStarted = computed(() => {
  if (!eventStartDate.value)
    return true // If no start date, show content
  return new Date() >= eventStartDate.value
})

// Computed submission status based on dates
const submissionStatus = computed(() => {
  const now = new Date()
  if (!submissionsStartDate.value)
    return 'upcoming'
  if (now < submissionsStartDate.value)
    return 'upcoming'
  if (submissionsEndDate.value && now > submissionsEndDate.value)
    return 'closed'
  return 'open'
})

// Fetch existing submissions for the team
const { data: submissionsData } = useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsListEndpoint(
  computed(() => hackathonId.value ?? ''),
  computed(() => teamId.value ?? ''),
)

// Check if team has any existing submissions
const hasExistingSubmissions = computed(() => {
  return (submissionsData.value?.submissions?.length ?? 0) > 0
})

// Get the existing submission (for displaying submitted values)
const existingSubmission = computed(() => {
  return submissionsData.value?.submissions?.[0] ?? null
})

// Form disabled when submissions not open
const isFormDisabled = computed(() =>
  submissionStatus.value !== 'open',
)

// Confirmation modal state
const showConfirmModal = ref(false)

// Form fields
const title = ref('')
const summary = ref('')
const repoUri = ref('')
const demoUri = ref('')
const slidesUri = ref('')
const location = ref('')

// URL validation
function isValidUrl(url: string): boolean {
  if (!url.trim())
    return true // Empty is valid (optional field)
  try {
    new URL(url)
    return true
  }
  catch {
    return false
  }
}

// Mutation to create submission
const createSubmissionMutation = useCreateSubmission(hackathonId, teamId)

const isSubmitting = computed(() => createSubmissionMutation.isPending.value)
const submissionErrorFields = ['repoUri', 'challengeId', 'slidesUri', 'title', 'summary', 'GeneralErrors']

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

  if (!summary.value.trim()) {
    toast.add({
      title: 'Summary is required',
      description: 'Please enter a summary for your submission.',
      color: 'error',
    })
    return
  }

  if (!repoUri.value.trim()) {
    toast.add({
      title: 'Repository URL is required',
      description: 'Please enter a repository URL for your submission.',
      color: 'error',
    })
    return
  }

  if (!slidesUri.value.trim()) {
    toast.add({
      title: 'Slides URL is required',
      description: 'Please enter a slides URL for your submission.',
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
  if (!challengeId.value)
    return

  createSubmissionMutation.mutate({
    challengeId: challengeId.value,
    title: title.value.trim(),
    summary: summary.value.trim() || null,
    repoUri: repoUri.value.trim() || null,
    demoUri: demoUri.value.trim() || null,
    slidesUri: slidesUri.value.trim() || null,
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
    onError(error) {
      toast.add({
        title: 'Failed to submit',
        description: getFastEndpointsErrorMessage(error, submissionErrorFields) ?? 'Please try again.',
        color: 'error',
      })
    },
  })
}
</script>

<template>
  <section class="w-full">
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-[linear-gradient(to_right,#FBFFAA_0%,#FFCD7D_94%,#FFFEAA_100%)] border-y border-y-black" style="border-image: linear-gradient(to right, black, transparent) 1">
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

    <!-- Hackathon hasn't started yet -->
    <div
      v-else-if="!hasHackathonStarted"
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <p class="font-['Raleway'] text-base lg:text-xl text-center">
        Submission will be available when the hackathon begins.
      </p>
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
        <!-- Submitted: show submission details -->
        <template v-if="hasExistingSubmissions">
          <div class="mt-6 p-4 bg-green-50 border border-green-200 rounded-lg flex items-center gap-2">
            <UIcon name="i-lucide-circle-check" class="text-green-600 size-5 shrink-0" />
            <p class="font-['Raleway'] text-base lg:text-xl text-green-800">
              Your team has successfully submitted a project.
            </p>
          </div>

          <div class="mt-8 space-y-5">
            <div>
              <h3 class="font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-1">
                Project Title
              </h3>
              <p class="font-['Raleway'] text-base lg:text-xl">
                {{ existingSubmission?.title }}
              </p>
            </div>

            <div v-if="existingSubmission?.summary">
              <h3 class="font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-1">
                Summary
              </h3>
              <p class="font-['Raleway'] text-base lg:text-xl whitespace-pre-line">
                {{ existingSubmission.summary }}
              </p>
            </div>

            <div v-if="existingSubmission?.repoUri">
              <h3 class="font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-1">
                Repository URL
              </h3>
              <a :href="existingSubmission.repoUri" target="_blank" rel="noopener noreferrer" class="font-['Raleway'] text-base lg:text-xl text-blue-600 underline break-all">
                {{ existingSubmission.repoUri }}
              </a>
            </div>

            <div v-if="existingSubmission?.slidesUri">
              <h3 class="font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-1">
                Slides URL
              </h3>
              <a :href="existingSubmission.slidesUri" target="_blank" rel="noopener noreferrer" class="font-['Raleway'] text-base lg:text-xl text-blue-600 underline break-all">
                {{ existingSubmission.slidesUri }}
              </a>
            </div>

            <div v-if="existingSubmission?.demoUri">
              <h3 class="font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-1">
                Demo URL
              </h3>
              <a :href="existingSubmission.demoUri" target="_blank" rel="noopener noreferrer" class="font-['Raleway'] text-base lg:text-xl text-blue-600 underline break-all">
                {{ existingSubmission.demoUri }}
              </a>
            </div>

            <div v-if="existingSubmission?.location">
              <h3 class="font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-1">
                Location
              </h3>
              <p class="font-['Raleway'] text-base lg:text-xl">
                {{ existingSubmission.location }}
              </p>
            </div>
          </div>
        </template>

        <!-- Not yet submitted -->
        <template v-else>
          <!-- Status messages -->
          <div
            v-if="submissionStatus === 'upcoming'"
            class="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg"
          >
            <p class="font-['Raleway'] text-base lg:text-xl text-yellow-800">
              Project submissions are not open yet.
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
                Summary *
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
                Repository URL *
              </label>
              <input
                v-model="repoUri"
                type="url"
                class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
                placeholder="https://github.com/your-repo"
                :disabled="isFormDisabled"
              >
            </div>

            <!-- Slides URL -->
            <div>
              <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
                Slides URL *
              </label>
              <input
                v-model="slidesUri"
                type="url"
                class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
                placeholder="https://slides.google.com/..."
                :disabled="isFormDisabled"
              >
            </div>

            <!-- Demo URL -->
            <div>
              <label class="block font-['Zalando_Sans_Expanded'] text-base lg:text-xl font-bold mb-2">
                Demo URL <span class="font-normal text-black/50">(optional)</span>
              </label>
              <input
                v-model="demoUri"
                type="url"
                class="w-full bg-transparent border border-black/20 rounded px-4 py-3 font-['Raleway'] text-base lg:text-xl focus:outline-none focus:border-black/40 disabled:opacity-50 disabled:cursor-not-allowed"
                placeholder="https://your-demo-link.com"
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
                placeholder="e.g., Event Hall 2-1"
                :disabled="isFormDisabled"
              >
            </div>

            <!-- Submit Button -->
            <div class="flex gap-4 mt-8">
              <button
                class="flex-1 py-3 bg-linear-to-r from-[#4B8BF5] via-[#7DB4FF] to-[#AAD4FF] text-black rounded font-['Zalando_Sans_Expanded'] text-base lg:text-xl hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
                :disabled="isFormDisabled || isSubmitting || !challengeId"
                @click="openConfirmModal"
              >
                Submit
              </button>
            </div>
          </div>
        </template>
      </div>
    </div>

    <!-- Confirmation Modal -->
    <UModal v-model:open="showConfirmModal">
      <template #content>
        <div class="p-6">
          <h3 class="font-['Zalando_Sans_Expanded'] text-lg font-bold mb-2">
            Confirm Submission
          </h3>
          <p class="font-['Raleway'] text-base mb-1 text-gray-600">
            Please review your submission details carefully.
          </p>
          <p class="font-['Raleway'] text-base mb-4 text-red-600">
            This cannot be changed after submission.
          </p>

          <!-- Submission Details -->
          <div class="space-y-3 mb-6 font-['Raleway'] text-sm break-all">
            <div>
              <span class="font-bold">Project Title:</span> {{ title }}
            </div>
            <div v-if="summary">
              <span class="font-bold">Summary:</span> {{ summary }}
            </div>
            <div v-if="repoUri">
              <span class="font-bold">Repository:</span> {{ repoUri }}
            </div>
            <div v-if="slidesUri">
              <span class="font-bold">Slides:</span> {{ slidesUri }}
            </div>
            <div v-if="demoUri">
              <span class="font-bold">Demo:</span> {{ demoUri }}
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
