<script setup lang="ts">
import { ref, computed } from 'vue'
import FormBox from './FormBox.vue'
import FormField from './FormField.vue'

const router = useRouter()

// Skills fields
const github = ref('')
const portfolio = ref('')
const desiredRole = ref('')
const areaOfExpertise = ref('')

const desiredRoleOptions = [
  { value: 'frontend', label: 'Frontend Developer' },
  { value: 'backend', label: 'Backend Developer' },
  { value: 'fullstack', label: 'Full Stack Developer' },
  { value: 'designer', label: 'Designer' },
  { value: 'product', label: 'Product Manager' },
]

// Logistics fields
const dietaryRestrictions = ref('')
const foodAllergies = ref('')
const foodAllergiesSpecify = ref('')
const tShirtSize = ref('')

const dietaryRestrictionsOptions = [
  { value: 'none', label: 'None' },
  { value: 'vegetarian', label: 'Vegetarian' },
  { value: 'vegan', label: 'Vegan' },
  { value: 'halal', label: 'Halal' },
  { value: 'kosher', label: 'Kosher' },
]

const foodAllergiesOptions = [
  { value: 'none', label: 'None' },
  { value: 'peanuts', label: 'Peanuts' },
  { value: 'shellfish', label: 'Shellfish' },
  { value: 'dairy', label: 'Dairy' },
  { value: 'gluten', label: 'Gluten' },
  { value: 'other', label: 'Other' },
]

const tShirtSizeOptions = [
  { value: 'xs', label: 'XS' },
  { value: 's', label: 'S' },
  { value: 'm', label: 'M' },
  { value: 'l', label: 'L' },
  { value: 'xl', label: 'XL' },
  { value: 'xxl', label: 'XXL' },
]

// Check if all required fields are filled
const isFormValid = computed(() => {
  const skillsValid = github.value && portfolio.value && desiredRole.value && areaOfExpertise.value
  const logisticsValid = dietaryRestrictions.value && foodAllergies.value && tShirtSize.value
  const foodAllergiesValid = foodAllergies.value !== 'other' || foodAllergiesSpecify.value

  return skillsValid && logisticsValid && foodAllergiesValid
})

// Navigate to next step
const handleNext = () => {
  if (isFormValid.value) {
    router.push({
      path: '/registration/form',
      query: { index: '2' },
    })
  }
}
</script>

<template>
  <div class="w-full mt-[64px] mb-[120px] flex justify-center bg-white">
    <div class="flex flex-col gap-[32px]">
      <!-- First Form Box: Skills -->
      <FormBox
        title="Skills"
        icon-width="28.5px"
        icon-height="40px"
        icon-src="/logos/skills.svg"
      >
        <!-- Row 1: Github and Portfolio -->
        <div class="flex gap-[12px]">
          <FormField
            v-model="github"
            label="Github"
            width="normal"
          />
          <FormField
            v-model="portfolio"
            label="Portfolio / Personal Website"
            width="normal"
          />
        </div>

        <!-- Row 2: Desired Role and Area of Expertise -->
        <div class="flex gap-[12px]">
          <FormField
            v-model="desiredRole"
            label="Desired Role"
            width="normal"
            type="select"
            placeholder="Select Option"
            :options="desiredRoleOptions"
          />
          <FormField
            v-model="areaOfExpertise"
            label="Area of Expertise"
            width="normal"
          />
        </div>
      </FormBox>

      <!-- Second Form Box: Logistics -->
      <div class="mt-[64px]">
        <FormBox
          title="Logistics"
          icon-width="41px"
          icon-height="46px"
          icon-src="/logos/logistics.svg"
        >
          <!-- Row 1: Dietary Restrictions and Food Allergies -->
          <div class="flex gap-[12px]">
            <FormField
              v-model="dietaryRestrictions"
              label="Dietary Restrictions"
              width="normal"
              type="select"
              placeholder="Select Option"
              :options="dietaryRestrictionsOptions"
            />
            <div class="flex flex-col gap-[12px]">
              <FormField
                v-model="foodAllergies"
                label="Food Allergies"
                width="normal"
                type="select"
                placeholder="Select Option"
                :options="foodAllergiesOptions"
              />
              <!-- Conditional Field: Please Specify (if Food Allergies is Other) -->
              <FormField
                v-if="foodAllergies === 'other'"
                v-model="foodAllergiesSpecify"
                label="Please Specify"
                width="normal"
              />
            </div>
          </div>

          <!-- Row 2: T-Shirt Size -->
          <FormField
            v-model="tShirtSize"
            label="T-Shirt Size"
            width="normal"
            type="select"
            placeholder="Select Option"
            :options="tShirtSizeOptions"
          />

          <!-- T-Shirt Size Reference Image -->
          <img
            src="/logos/shirtsizes.png"
            alt="T-Shirt Size Reference"
            class="w-[502px] h-[324px]"
          >
        </FormBox>
      </div>

      <!-- Back and Next Buttons -->
      <div class="mt-[64px] flex justify-between items-center">
        <!-- Back Button -->
        <a
          href="/registration/form?index=0"
          class="font-zalando text-[20px] font-normal text-black underline cursor-pointer"
        >
          Back
        </a>

        <!-- Next Button -->
        <button
          :disabled="!isFormValid"
          :class="[
            'w-[148.61px] h-[48px] font-zalando text-[20px] font-normal rounded-[8px] flex items-center justify-center gap-2',
            isFormValid ? 'bg-black text-white cursor-pointer' : 'bg-gray-300 text-gray-500 cursor-not-allowed',
          ]"
          @click="handleNext"
        >
          NEXT
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
