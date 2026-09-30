<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryKey,
  geeksHackingPortalApiEndpointsUsersProfileGetEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsUsersProfileUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

const toast = useToast()
const queryClient = useQueryClient()
const { data: user, isLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const updateProfileMutation = useGeeksHackingPortalApiEndpointsUsersProfileUpdateEndpoint()

const form = reactive({
  firstName: '',
  lastName: '',
})

watch(user, (value) => {
  if (!value)
    return

  form.firstName = value.firstName ?? ''
  form.lastName = value.lastName ?? ''
}, { immediate: true })

const isValid = computed(() =>
  form.firstName.trim() !== '' && form.lastName.trim() !== '',
)

const isDirty = computed(() =>
  form.firstName.trim() !== (user.value?.firstName ?? '')
  || form.lastName.trim() !== (user.value?.lastName ?? ''),
)

const isSubmitting = computed(() => updateProfileMutation.isPending.value)

async function handleSubmit() {
  if (!isValid.value) {
    toast.add({
      title: 'Name is required',
      description: 'Enter both a first name and a last name.',
      color: 'error',
    })
    return
  }

  if (!isDirty.value)
    return

  const firstName = form.firstName.trim()
  const lastName = form.lastName.trim()

  try {
    await updateProfileMutation.mutateAsync({ body: { firstName, lastName } })
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryKey(),
      }),
      queryClient.invalidateQueries({
        queryKey: geeksHackingPortalApiEndpointsUsersProfileGetEndpointQueryKey(),
      }),
    ])

    toast.add({
      title: 'Profile updated',
      description: 'Your name will be used across registrations, emails, and organizer views.',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to update profile', error)
    toast.add({
      title: 'Could not update profile',
      description: 'Please try again in a moment.',
      color: 'error',
    })
  }
}
</script>

<template>
  <UDashboardPanel id="profile">
    <template #header>
      <UDashboardNavbar title="Profile">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="mx-auto flex w-full max-w-2xl flex-col gap-6">
        <div class="space-y-1">
          <h1 class="text-2xl font-semibold tracking-tight text-default">
            Your profile
          </h1>
          <p class="text-sm leading-6 text-muted">
            Your name belongs to your account, not to a single registration. Changes apply to every hackathon and workshop you join.
          </p>
        </div>

        <UCard v-if="isLoading">
          <div class="space-y-3">
            <USkeleton class="h-5 w-1/3" />
            <USkeleton class="h-10 w-full" />
            <USkeleton class="h-10 w-full" />
          </div>
        </UCard>

        <UCard v-else-if="user">
          <template #header>
            <div class="space-y-1">
              <h2 class="text-sm font-semibold">
                Account details
              </h2>
              <p class="text-sm text-muted">
                GitHub identity is managed by your GitHub account and cannot be edited here.
              </p>
            </div>
          </template>

          <form
            class="space-y-5"
            @submit.prevent="handleSubmit"
          >
            <div class="grid gap-4 sm:grid-cols-2">
              <UFormField label="First name" required>
                <UInput
                  v-model="form.firstName"
                  autocomplete="given-name"
                  placeholder="Ada"
                />
              </UFormField>

              <UFormField label="Last name" required>
                <UInput
                  v-model="form.lastName"
                  autocomplete="family-name"
                  placeholder="Lovelace"
                />
              </UFormField>
            </div>

            <UFormField label="Email">
              <UInput
                :model-value="user.email"
                disabled
                autocomplete="email"
              />
            </UFormField>

            <UFormField label="GitHub">
              <UInput
                :model-value="user.gitHubLogin"
                disabled
              />
            </UFormField>

            <div class="flex items-center justify-end gap-2">
              <UButton
                type="submit"
                icon="i-lucide-save"
                :loading="isSubmitting"
                :disabled="!isValid || !isDirty"
              >
                Save profile
              </UButton>
            </div>
          </form>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
