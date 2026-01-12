<script setup lang="ts">
import { ref, computed } from 'vue'
import { useSubmitRegistration } from '~/composables/registration'
import FormBox from './FormBox.vue'
import FormField from './FormField.vue'

const router = useRouter()

// Get hackathon ID from route or context
// TODO: This should come from the registration flow context or route params
const hackathonId = ref<string | null>(null)

// Outreach fields
const howDidYouFindOut = ref('')
const signUpForUpdates = ref('')
const lookingForJob = ref('')

// Disclaimer
const disclaimerAccepted = ref(false)

const howDidYouFindOutOptions = [
  { value: 'social-media', label: 'Social Media' },
  { value: 'friend', label: 'Friend/Colleague' },
  { value: 'website', label: 'Website' },
  { value: 'email', label: 'Email' },
  { value: 'other', label: 'Other' },
]

const signUpForUpdatesOptions = [
  { value: 'yes', label: 'Yes' },
  { value: 'no', label: 'No' },
]

const lookingForJobOptions = [
  { value: 'yes', label: 'Yes' },
  { value: 'no', label: 'No' },
  { value: 'not-sure', label: 'Not Sure' },
]

// Check if all required fields are filled
const isFormValid = computed(() => {
  return howDidYouFindOut.value && signUpForUpdates.value && lookingForJob.value && disclaimerAccepted.value
})

// Use mutation for submitting registration
const { mutate: submitRegistration, isPending, isSuccess, isError, error } = useSubmitRegistration()

// Submit form
const handleSubmit = async () => {
  if (!isFormValid.value || !hackathonId.value) {
    return
  }

  // TODO: Map form fields to actual question IDs from the backend
  // For now, using placeholder question IDs that would need to be fetched
  // from the registration questions endpoint first
  const submissions = [
    {
      questionId: 'how-did-you-find-out',
      value: howDidYouFindOut.value,
    },
    {
      questionId: 'sign-up-for-updates',
      value: signUpForUpdates.value,
    },
    {
      questionId: 'looking-for-job',
      value: lookingForJob.value,
    },
  ]

  submitRegistration(
    {
      hackathonId: hackathonId.value,
      submissions,
    },
    {
      onSuccess: (response) => {
        console.log('Form submitted successfully!', response)
        // Navigate to success page or show success message
        router.push('/registration/success')
      },
      onError: (err) => {
        console.error('Error submitting form', err)
      },
    }
  )
}
</script>

<template>
  <div class="w-full mt-[64px] mb-[120px] flex justify-center bg-white">
    <div class="flex flex-col gap-[32px]">
      <!-- Outreach Form Box -->
      <FormBox
        title="Outreach"
        icon-width="46px"
        icon-height="46px"
        icon-src="/logos/outreach.svg"
      >
        <!-- How did you find out about this hackathon? -->
        <FormField
          v-model="howDidYouFindOut"
          label="How did you find out about this hackathon?"
          width="big"
          type="select"
          placeholder="Select Option"
          :options="howDidYouFindOutOptions"
        />

        <!-- Sign up for updates -->
        <FormField
          v-model="signUpForUpdates"
          label="Sign up for updates on future GeeksHacking & Partners' events?"
          width="big"
          type="select"
          placeholder="Select Option"
          :options="signUpForUpdatesOptions"
        />

        <!-- Looking for a job -->
        <FormField
          v-model="lookingForJob"
          label="Are you currently looking for a job?"
          width="big"
          type="select"
          placeholder="Select Option"
          :options="lookingForJobOptions"
        />
      </FormBox>

      <!-- Disclaimer Section -->
      <div class="mt-[64px] w-[502px]">
        <h3 class="font-raleway text-[20px] font-normal text-black mb-2">
          Disclaimer
        </h3>
        <p class="font-raleway text-[16px] font-normal text-black mb-4">
          Submitting this form does not guarantee you a spot at HackOMania 2026. Selected participants will receive a registration email from [email] within x business days of submitting this form.
        </p>
        <p class="font-raleway text-[16px] font-normal text-black mb-4">
          Please check your spam folder for the event registration email.
        </p>

        <!-- Checkbox -->
        <label class="flex items-center gap-3 cursor-pointer">
          <input
            v-model="disclaimerAccepted"
            type="checkbox"
            class="w-5 h-5 rounded-full border-2 border-black cursor-pointer accent-black"
          >
          <span class="font-raleway text-[16px] font-normal text-black">
            Yes, I have read and understood the disclaimer
          </span>
        </label>
      </div>

      <!-- Error Message -->
      <div v-if="isError" class="mt-4 p-4 bg-red-50 border border-red-200 rounded-[8px]">
        <p class="font-raleway text-[14px] text-red-600">
          {{ error?.message || 'An error occurred while submitting the form. Please try again.' }}
        </p>
      </div>

      <!-- Success Message -->
      <div v-if="isSuccess" class="mt-4 p-4 bg-green-50 border border-green-200 rounded-[8px]">
        <p class="font-raleway text-[14px] text-green-600">
          Form submitted successfully! Redirecting...
        </p>
      </div>

      <!-- Back and Submit Buttons -->
      <div class="mt-[64px] flex justify-between items-center">
        <!-- Back Button -->
        <a
          href="/registration/form?index=1"
          class="font-zalando text-[20px] font-normal text-black underline cursor-pointer"
        >
          Back
        </a>

        <!-- Submit Button -->
        <button
          :disabled="!isFormValid || isPending"
          :class="[
            'w-[148.61px] h-[48px] font-zalando text-[20px] font-normal rounded-[8px] flex items-center justify-center gap-2',
            isFormValid && !isPending ? 'bg-black text-white cursor-pointer' : 'bg-gray-300 text-gray-500 cursor-not-allowed',
          ]"
          @click="handleSubmit"
        >
          <span v-if="isPending">Submitting...</span>
          <span v-else>SUBMIT</span>
          <svg
            v-if="!isPending"
            xmlns="http://www.w3.org/2000/svg"
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <polyline points="9 18 15 12 9 6" />
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>
