<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  label: string
  modelValue: string
  width?: 'normal' | 'big'
  type?: 'text' | 'email' | 'tel' | 'select'
  options?: Array<{ value: string, label: string }>
  placeholder?: string
}>(), {
  width: 'normal',
  type: 'text',
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const widthClass = computed(() => {
  return props.width === 'big' ? 'w-[502px]' : 'w-[245px]'
})
</script>

<template>
  <div class="flex flex-col">
    <label class="font-raleway text-[16px] font-normal text-black">
      {{ label }}
    </label>
    <select
      v-if="type === 'select'"
      :value="modelValue"
      :class="[
        widthClass,
        'h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] bg-white text-gray-700'
      ]"
      @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
      <option
        value=""
        disabled
      >
        {{ placeholder || `Select ${label.toLowerCase()}` }}
      </option>
      <option
        v-for="option in options"
        :key="option.value"
        :value="option.value"
      >
        {{ option.label }}
      </option>
    </select>
    <input
      v-else
      :value="modelValue"
      :type="type"
      :class="[
        widthClass,
        'h-[40px] rounded-[8px] border border-black px-3 font-raleway text-[16px] text-gray-700 placeholder:text-gray-500'
      ]"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    >
  </div>
</template>
