<script setup lang="ts">
import { ref, computed } from 'vue'
import FormBox from './FormBox.vue'
import FormField from './FormField.vue'

const router = useRouter()

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

// Submit form
const handleSubmit = () => {
  if (isFormValid.value) {
    // Handle final submission
    console.log('Form submitted!')
  }
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
          :disabled="!isFormValid"
          :class="[
            'w-[148.61px] h-[48px] font-zalando text-[20px] font-normal rounded-[8px] flex items-center justify-center gap-2',
            isFormValid ? 'bg-black text-white cursor-pointer' : 'bg-gray-300 text-gray-500 cursor-not-allowed',
          ]"
          @click="handleSubmit"
        >
          SUBMIT
          <svg
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
