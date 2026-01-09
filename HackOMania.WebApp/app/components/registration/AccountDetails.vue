<script setup lang="ts">
import { ref, computed } from 'vue'

const router = useRouter()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const phoneNumber = ref('')
const telegramHandle = ref('')
const age = ref('')
const gender = ref('')
const nationality = ref('')
const occupation = ref('')

// Working-specific fields
const jobRole = ref('')
const company = ref('')
const yearsOfExperience = ref('')
const highestEducation = ref('')

// Student-specific fields
const schoolName = ref('')
const majorCourse = ref('')

// Check if all required fields are filled
const isFormValid = computed(() => {
  const baseFieldsFilled = firstName.value && lastName.value && email.value &&
    phoneNumber.value && telegramHandle.value && age.value &&
    gender.value && nationality.value && occupation.value

  if (!baseFieldsFilled) return false

  if (occupation.value === 'working') {
    return jobRole.value && yearsOfExperience.value && highestEducation.value
  }

  if (occupation.value === 'student') {
    return schoolName.value && majorCourse.value
  }

  return true
})

// Navigate to next step
const handleNext = () => {
  if (isFormValid.value) {
    router.push({
      path: '/registration/form',
      query: { index: '1' },
    })
  }
}
</script>

<template>
  <div class="w-full mt-[64px] mb-[120px] flex justify-center bg-white">
    <div class="w-auto">
      <!-- Header with icon and title -->
      <div class="flex items-center">
        <!-- Icon -->
        <img
          src="/logos/personaldetails.svg"
          alt="Personal Details"
          class="w-[40px] h-[40px]"
        >

        <!-- Title -->
        <h2 class="ml-[12px] font-raleway text-[20px] font-normal text-black">
          Personal Details
        </h2>
      </div>

      <!-- Form content -->
      <div class="mt-[16px] flex flex-col gap-[12px]">
        <!-- Row 1: First Name and Last Name -->
        <div class="flex gap-[12px]">
          <div class="flex flex-col">
            <label class="font-raleway text-[16px] font-normal text-black">
              First Name
            </label>
            <input
              v-model="firstName"
              type="text"
              class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
            >
          </div>
          <div class="flex flex-col">
            <label class="font-raleway text-[16px] font-normal text-black">
              Last Name
            </label>
            <input
              v-model="lastName"
              type="text"
              class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
            >
          </div>
        </div>

        <!-- Row 2: Email Address -->
        <div class="flex flex-col">
          <label class="font-raleway text-[16px] font-normal text-black">
            Email Address
          </label>
          <input
            v-model="email"
            type="email"
            class="w-[502px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
          >
        </div>

        <!-- Row 3: Phone Number -->
        <div class="flex flex-col">
          <label class="font-raleway text-[16px] font-normal text-black">
            Phone Number
          </label>
          <input
            v-model="phoneNumber"
            type="tel"
            class="w-[502px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
          >
        </div>

        <!-- Row 4: Telegram Handle -->
        <div class="flex flex-col">
          <label class="font-raleway text-[16px] font-normal text-black">
            Telegram Handle
          </label>
          <input
            v-model="telegramHandle"
            type="text"
            class="w-[502px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
          >
        </div>

        <!-- Row 5: Age and Gender -->
        <div class="flex gap-[12px]">
          <div class="flex flex-col">
            <label class="font-raleway text-[16px] font-normal text-black">
              Age
            </label>
            <input
              v-model="age"
              type="text"
              class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
            >
          </div>
          <div class="flex flex-col">
            <label class="font-raleway text-[16px] font-normal text-black">
              Gender
            </label>
            <select
              v-model="gender"
              class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] bg-white text-gray-700"
            >
              <option
                value=""
                disabled
              >
                Select Option
              </option>
              <option value="male">
                Male
              </option>
              <option value="female">
                Female
              </option>
              <option value="other">
                Other
              </option>
              <option value="prefer-not-to-say">
                Prefer not to say
              </option>
            </select>
          </div>
        </div>

        <!-- Row 6: Nationality -->
        <div class="flex flex-col">
          <label class="font-raleway text-[16px] font-normal text-black">
            Nationality
          </label>
          <select
            v-model="nationality"
            class="w-[502px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] bg-white text-gray-700"
          >
            <option
              value=""
              disabled
            >
              Select Option
            </option>
            <option value="singaporean">
              Singaporean
            </option>
            <option value="malaysian">
              Malaysian
            </option>
            <option value="other">
              Other
            </option>
          </select>
        </div>

        <!-- Row 7: Occupation -->
        <div class="flex flex-col">
          <label class="font-raleway text-[16px] font-normal text-black">
            Occupation
          </label>
          <select
            v-model="occupation"
            class="w-[502px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] bg-white text-gray-700"
          >
            <option
              value=""
              disabled
            >
              Select Option
            </option>
            <option value="working">
              Working
            </option>
            <option value="student">
              Student
            </option>
            <option value="unemployed">
              Unemployed/In between Jobs
            </option>
          </select>
        </div>

        <!-- Conditional fields for "Working" -->
        <template v-if="occupation === 'working'">
          <!-- Job Role and Company -->
          <div class="flex gap-[12px]">
            <div class="flex flex-col">
              <label class="font-raleway text-[16px] font-normal text-black">
                Job Role
              </label>
              <input
                v-model="jobRole"
                type="text"
                class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
              >
            </div>
            <div class="flex flex-col">
              <label class="font-raleway text-[16px] font-normal text-black">
                Company (Optional)
              </label>
              <input
                v-model="company"
                type="text"
                class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
              >
            </div>
          </div>

          <!-- Years of Experience and Highest Level of Education -->
          <div class="flex gap-[12px]">
            <div class="flex flex-col">
              <label class="font-raleway text-[16px] font-normal text-black">
                Years of Experience
              </label>
              <input
                v-model="yearsOfExperience"
                type="text"
                class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
              >
            </div>
            <div class="flex flex-col">
              <label class="font-raleway text-[16px] font-normal text-black">
                Highest Level of Education
              </label>
              <select
                v-model="highestEducation"
                class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] bg-white text-gray-700"
              >
                <option
                  value=""
                  disabled
                >
                  Select Option
                </option>
                <option value="secondary-school">
                  Secondary School
                </option>
                <option value="diploma">
                  Diploma
                </option>
                <option value="bachelors">
                  Bachelor's Degree
                </option>
                <option value="masters">
                  Master's Degree
                </option>
                <option value="phd">
                  PhD
                </option>
              </select>
            </div>
          </div>
        </template>

        <!-- Conditional fields for "Student" -->
        <template v-if="occupation === 'student'">
          <!-- School Name and Major / Course -->
          <div class="flex gap-[12px]">
            <div class="flex flex-col">
              <label class="font-raleway text-[16px] font-normal text-black">
                School Name
              </label>
              <input
                v-model="schoolName"
                type="text"
                class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
              >
            </div>
            <div class="flex flex-col">
              <label class="font-raleway text-[16px] font-normal text-black">
                Major / Course
              </label>
              <input
                v-model="majorCourse"
                type="text"
                class="w-[245px] h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500"
              >
            </div>
          </div>
        </template>

        <!-- Next Button -->
        <div class="mt-[64px] flex justify-end">
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
  </div>
</template>
