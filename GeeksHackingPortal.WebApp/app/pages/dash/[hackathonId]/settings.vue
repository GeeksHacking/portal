<script setup lang="ts">
import { useQueryClient } from '@tanstack/vue-query'
import {
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint,
  geeksHackingPortalApiEndpointsOrganizersHackathonGetEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref, watch } from 'vue'

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? '')

const { data: hackathon, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint(
  hackathonId,
  { query: { enabled: computed(() => !!hackathonId.value) } },
)

const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint()

const form = ref({
  enableRepositoryChecking: false,
  enableRepositoryForking: false,
  hasStoredApiKey: false,
  apiKey: '',
  clearStoredApiKey: false,
  repositoryPrefix: '',
  organizationId: '',
})

watch(
  hackathon,
  (value) => {
    if (!value)
      return

    form.value = {
      enableRepositoryChecking: value.gitHubRepositorySettings?.isRepositoryCheckingEnabled ?? false,
      enableRepositoryForking: value.gitHubRepositorySettings?.isRepositoryForkingEnabled ?? false,
      hasStoredApiKey: value.gitHubRepositorySettings?.hasApiKey ?? false,
      apiKey: '',
      clearStoredApiKey: false,
      repositoryPrefix: value.gitHubRepositorySettings?.repositoryPrefix ?? '',
      organizationId: value.gitHubRepositorySettings?.organizationId?.toString() ?? '',
    }
  },
  { immediate: true },
)

const isSubmitting = computed(() => updateMutation.isPending.value)

async function handleSubmit() {
  const trimmedApiKey = form.value.apiKey.trim()
  const organizationIdValue = form.value.organizationId.trim()
  const hasExistingApiKeyAfterSubmit = form.value.hasStoredApiKey && !form.value.clearStoredApiKey
  const requiresApiKey = form.value.enableRepositoryChecking || form.value.enableRepositoryForking

  if (requiresApiKey && !trimmedApiKey && !hasExistingApiKeyAfterSubmit) {
    toast.add({
      title: 'GitHub API key required',
      description: 'Provide an API key before enabling repository checking or cloning.',
      color: 'error',
    })
    return
  }

  if (form.value.enableRepositoryForking && !organizationIdValue) {
    toast.add({
      title: 'Organization ID required',
      description: 'Provide a GitHub organization id before enabling repository cloning.',
      color: 'error',
    })
    return
  }

  const organizationId = organizationIdValue ? Number(organizationIdValue) : undefined
  if (organizationIdValue && (!Number.isInteger(organizationId) || organizationId! <= 0)) {
    toast.add({
      title: 'Invalid organization id',
      description: 'GitHub organization id must be a positive integer.',
      color: 'error',
    })
    return
  }

  try {
    const result = await updateMutation.mutateAsync({
      activityId: hackathonId.value,
      data: {
        gitHubRepositorySettings: {
          isRepositoryCheckingEnabled: form.value.enableRepositoryChecking,
          isRepositoryForkingEnabled: form.value.enableRepositoryForking,
          apiKey: trimmedApiKey || undefined,
          clearApiKey: form.value.clearStoredApiKey || undefined,
          repositoryPrefix: form.value.repositoryPrefix.trim() || undefined,
          organizationId,
        },
      },
    })

    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonGetEndpointQueryKey(hackathonId.value),
    })

    form.value.apiKey = ''
    form.value.clearStoredApiKey = false
    form.value.hasStoredApiKey = result?.gitHubRepositorySettings?.hasApiKey ?? false

    toast.add({
      title: 'GitHub settings updated',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to update GitHub settings', error)
    toast.add({
      title: 'Failed to update GitHub settings',
      description: 'Please review the settings and try again.',
      color: 'error',
    })
  }
}
</script>

<template>
  <UDashboardPanel id="settings">
    <template #header>
      <UDashboardNavbar title="Settings">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <UCard>
          <template #header>
            <div class="space-y-1">
              <h3 class="text-sm font-semibold">
                GitHub Submission Automation
              </h3>
              <p class="text-sm text-(--ui-text-muted)">
                Validate submitted repositories with GitHub and optionally fork them into your organization when teams submit.
              </p>
            </div>
          </template>

          <div
            v-if="isLoading"
            class="text-sm text-(--ui-text-muted)"
          >
            Loading settings...
          </div>

          <form
            v-else
            class="space-y-5"
            @submit.prevent="handleSubmit"
          >
            <div class="grid gap-4 lg:grid-cols-2">
              <UCard variant="subtle">
                <div class="space-y-3">
                  <UCheckbox
                    v-model="form.enableRepositoryChecking"
                    label="Validate repository links on submission"
                  />
                  <UCheckbox
                    v-model="form.enableRepositoryForking"
                    label="Fork repositories into the configured GitHub organization"
                  />
                </div>
              </UCard>

              <UCard variant="subtle">
                <div class="space-y-2 text-sm">
                  <div class="flex items-center justify-between gap-3">
                    <span class="text-(--ui-text-muted)">Stored API key</span>
                    <UBadge
                      :color="form.hasStoredApiKey && !form.clearStoredApiKey ? 'success' : 'warning'"
                      variant="subtle"
                      size="sm"
                    >
                      {{ form.hasStoredApiKey && !form.clearStoredApiKey ? 'Configured' : 'Missing' }}
                    </UBadge>
                  </div>
                  <p class="text-(--ui-text-muted)">
                    The key is only used server-side for repository validation and forking. Leave the field blank to keep the current key.
                  </p>
                </div>
              </UCard>
            </div>

            <UFormField label="GitHub API Key">
              <UInput
                v-model="form.apiKey"
                type="password"
                autocomplete="new-password"
                placeholder="ghp_..."
              />
            </UFormField>

            <UCheckbox
              v-model="form.clearStoredApiKey"
              label="Remove the currently stored API key"
            />

            <div class="grid gap-4 lg:grid-cols-2">
              <UFormField label="Forked Repository Prefix">
                <UInput
                  v-model="form.repositoryPrefix"
                  placeholder="hackomania-"
                />
              </UFormField>

              <UFormField label="GitHub Organization ID">
                <UInput
                  v-model="form.organizationId"
                  inputmode="numeric"
                  placeholder="123456789"
                />
              </UFormField>
            </div>

            <div class="flex justify-end">
              <UButton
                type="submit"
                :loading="isSubmitting"
              >
                Save Settings
              </UButton>
            </div>
          </form>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
