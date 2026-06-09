<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationPlatform,
  GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationResponse,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsUpdateEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsAnalyticsEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { computed, ref } from 'vue'
import { getApiErrorMessage } from '~/utils/api-errors'

type OAuthApplication = GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationResponse
type OAuthApplicationPlatform = GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationPlatform

interface ApplicationForm {
  clientId: string
  displayName: string
  platform: OAuthApplicationPlatform
  redirectUrisText: string
  postLogoutRedirectUrisText: string
  rotateClientSecret: boolean
}

const toast = useToast()
const queryClient = useQueryClient()

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: applicationsData, isLoading: isLoadingApplications } = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpoint({
  query: {
    enabled: computed(() => user.value?.isRoot === true),
  },
})

const { data: analyticsData, isLoading: isLoadingAnalytics } = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsAnalyticsEndpoint({
  query: {
    enabled: computed(() => user.value?.isRoot === true),
  },
})

const analyticsMap = computed(() => {
  const map = new Map()
  if (analyticsData.value?.items) {
    for (const item of analyticsData.value.items) {
      map.set(item.applicationId, item)
    }
  }
  return map
})

const createMutation = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsUpdateEndpoint()
const deleteMutation = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsDeleteEndpoint()

const platformItems = [
  { label: 'Web', value: 'Web' },
  { label: 'Native', value: 'Native' },
]

const applications = computed(() => applicationsData.value?.items ?? [])
const isEditorOpen = ref(false)
const isEditing = ref(false)
const editingApplicationId = ref<string | null>(null)
const pendingDeleteApplication = ref<OAuthApplication | null>(null)
const issuedSecret = ref<string | null>(null)

const form = ref<ApplicationForm>({
  clientId: '',
  displayName: '',
  platform: 'Web',
  redirectUrisText: '',
  postLogoutRedirectUrisText: '',
  rotateClientSecret: false,
})

const isSubmitting = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

function resetForm() {
  form.value = {
    clientId: '',
    displayName: '',
    platform: 'Web',
    redirectUrisText: '',
    postLogoutRedirectUrisText: '',
    rotateClientSecret: false,
  }
  issuedSecret.value = null
  isEditing.value = false
  editingApplicationId.value = null
}

function openCreateModal() {
  resetForm()
  isEditorOpen.value = true
}

function openEditModal(application: OAuthApplication) {
  form.value = {
    clientId: application.clientId ?? '',
    displayName: application.displayName ?? '',
    platform: application.platform ?? 'Web',
    redirectUrisText: (application.redirectUris ?? []).join('\n'),
    postLogoutRedirectUrisText: (application.postLogoutRedirectUris ?? []).join('\n'),
    rotateClientSecret: false,
  }
  issuedSecret.value = null
  isEditing.value = true
  editingApplicationId.value = application.id ?? null
  isEditorOpen.value = true
}

function parseUriLines(value: string) {
  return value
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)
}

function buildPayload() {
  return {
    clientId: form.value.clientId.trim(),
    displayName: form.value.displayName.trim(),
    platform: form.value.platform,
    redirectUris: parseUriLines(form.value.redirectUrisText),
    postLogoutRedirectUris: parseUriLines(form.value.postLogoutRedirectUrisText),
  }
}

async function handleSubmit() {
  const payload = buildPayload()
  if (!payload.clientId || !payload.displayName || payload.redirectUris.length === 0) {
    toast.add({
      title: 'Missing required fields',
      color: 'error',
    })
    return
  }

  try {
    const response = isEditing.value && editingApplicationId.value
      ? await updateMutation.mutateAsync({
          id: editingApplicationId.value,
          data: {
            ...payload,
            rotateClientSecret: form.value.platform === 'Web' && form.value.rotateClientSecret,
          },
        })
      : await createMutation.mutateAsync({ data: payload })

    issuedSecret.value = response.clientSecret ?? null
    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpointQueryKey(),
    })

    toast.add({
      title: isEditing.value ? 'OAuth application updated' : 'OAuth application created',
      color: 'success',
    })

    if (!response.clientSecret) {
      isEditorOpen.value = false
      resetForm()
    }
  }
  catch (error) {
    toast.add({
      title: 'Could not save OAuth application',
      description: getApiErrorMessage(error, 'Please review the fields and try again.'),
      color: 'error',
    })
  }
}

async function confirmDelete() {
  if (!pendingDeleteApplication.value?.id)
    return

  try {
    await deleteMutation.mutateAsync({ id: pendingDeleteApplication.value.id })
    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpointQueryKey(),
    })
    toast.add({ title: 'OAuth application deleted', color: 'success' })
    pendingDeleteApplication.value = null
  }
  catch (error) {
    toast.add({
      title: 'Could not delete OAuth application',
      description: getApiErrorMessage(error, 'Please try again.'),
      color: 'error',
    })
  }
}

async function copySecret() {
  if (!issuedSecret.value)
    return

  await navigator.clipboard.writeText(issuedSecret.value)
  toast.add({ title: 'Client secret copied', color: 'success' })
}

function platformBadgeColor(platform: OAuthApplicationPlatform | null | undefined) {
  return platform === 'Native' ? 'info' : 'primary'
}
</script>

<template>
  <UDashboardPanel id="oauth-applications">
    <template #header>
      <UDashboardNavbar title="OAuth Applications">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
        <template #right>
          <UButton
            v-if="user?.isRoot"
            icon="i-lucide-plus"
            size="sm"
            @click="openCreateModal"
          >
            Add Application
          </UButton>
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div
        v-if="isLoadingUser || isLoadingApplications || isLoadingAnalytics"
        class="text-sm text-(--ui-text-muted)"
      >
        Loading OAuth applications...
      </div>

      <UAlert
        v-else-if="!user?.isRoot"
        color="error"
        variant="subtle"
        icon="i-lucide-shield-alert"
        title="Admin access required"
      />

      <div
        v-else-if="applications.length === 0"
        class="flex min-h-72 flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-default p-6 text-center"
      >
        <UIcon
          name="i-lucide-key-round"
          class="size-8 text-(--ui-text-muted)"
        />
        <div class="space-y-1">
          <h2 class="text-base font-semibold">
            No OAuth applications
          </h2>
          <p class="text-sm text-(--ui-text-muted)">
            Create the first client application.
          </p>
        </div>
        <UButton
          icon="i-lucide-plus"
          @click="openCreateModal"
        >
          Add Application
        </UButton>
      </div>

      <div
        v-else
        class="grid gap-4 lg:grid-cols-2"
      >
        <UCard
          v-for="application in applications"
          :key="application.id"
          :ui="{ body: 'space-y-4' }"
        >
          <template #header>
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <h2 class="truncate text-base font-semibold">
                  {{ application.displayName }}
                </h2>
                <p class="truncate text-sm text-(--ui-text-muted)">
                  {{ application.clientId }}
                </p>
              </div>
              <UBadge
                :color="platformBadgeColor(application.platform)"
                variant="subtle"
              >
                {{ application.platform }}
              </UBadge>
            </div>
          </template>

          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col rounded-lg border border-default p-3">
              <div class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Authorizations
              </div>
              <div class="mt-1 text-2xl font-semibold">
                {{ analyticsMap.get(application.id)?.totalAuthorizations ?? 0 }}
              </div>
            </div>
            <div class="flex flex-col rounded-lg border border-default p-3">
              <div class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Unique Users
              </div>
              <div class="mt-1 text-2xl font-semibold">
                {{ analyticsMap.get(application.id)?.uniqueUsers ?? 0 }}
              </div>
            </div>
          </div>

          <div class="space-y-3">
            <div>
              <div class="mb-1 text-xs font-medium uppercase text-(--ui-text-muted)">
                Redirect URIs
              </div>
              <div class="space-y-1">
                <code
                  v-for="uri in application.redirectUris"
                  :key="uri"
                  class="block truncate rounded-md bg-muted px-2 py-1 text-xs"
                >
                  {{ uri }}
                </code>
              </div>
            </div>

            <div v-if="application.postLogoutRedirectUris?.length">
              <div class="mb-1 text-xs font-medium uppercase text-(--ui-text-muted)">
                Post Logout Redirect URIs
              </div>
              <div class="space-y-1">
                <code
                  v-for="uri in application.postLogoutRedirectUris"
                  :key="uri"
                  class="block truncate rounded-md bg-muted px-2 py-1 text-xs"
                >
                  {{ uri }}
                </code>
              </div>
            </div>
          </div>

          <template #footer>
            <div class="flex justify-end gap-2">
              <UButton
                variant="ghost"
                icon="i-lucide-history"
                size="sm"
                :to="`/dash/oauth-applications/${application.id}`"
              >
                History
              </UButton>
              <UButton
                variant="ghost"
                icon="i-lucide-pencil"
                size="sm"
                @click="openEditModal(application)"
              >
                Edit
              </UButton>
              <UButton
                color="error"
                variant="ghost"
                icon="i-lucide-trash-2"
                size="sm"
                @click="pendingDeleteApplication = application"
              >
                Delete
              </UButton>
            </div>
          </template>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>

  <UModal v-model:open="isEditorOpen">
    <template #content>
      <UCard>
        <template #header>
          <div class="flex items-center justify-between gap-3">
            <h2 class="text-base font-semibold">
              {{ isEditing ? 'Edit OAuth Application' : 'Create OAuth Application' }}
            </h2>
            <UButton
              variant="ghost"
              icon="i-lucide-x"
              size="xs"
              @click="isEditorOpen = false"
            />
          </div>
        </template>

        <form
          class="space-y-4"
          @submit.prevent="handleSubmit"
        >
          <UAlert
            v-if="issuedSecret"
            color="warning"
            variant="subtle"
            icon="i-lucide-key-round"
            title="Client secret issued"
          >
            <template #description>
              <div class="mt-2 flex flex-col gap-2">
                <code class="break-all rounded-md bg-default px-2 py-1 text-xs">
                  {{ issuedSecret }}
                </code>
                <UButton
                  icon="i-lucide-copy"
                  size="xs"
                  variant="outline"
                  class="self-start"
                  @click="copySecret"
                >
                  Copy Secret
                </UButton>
              </div>
            </template>
          </UAlert>

          <div class="grid gap-4 sm:grid-cols-2">
            <LazyUFormField
              label="Client ID"
              required
            >
              <LazyUInput
                v-model="form.clientId"
                placeholder="my-client"
              />
            </LazyUFormField>

            <LazyUFormField
              label="Platform"
              required
            >
              <LazyUSelect
                v-model="form.platform"
                :items="platformItems"
                value-key="value"
                label-key="label"
              />
            </LazyUFormField>
          </div>

          <LazyUFormField
            label="Display Name"
            required
          >
            <LazyUInput
              v-model="form.displayName"
              placeholder="My Client Application"
            />
          </LazyUFormField>

          <LazyUFormField
            label="Redirect URIs"
            required
          >
            <LazyUTextarea
              v-model="form.redirectUrisText"
              :rows="4"
              placeholder="https://app.example.com/callback"
            />
          </LazyUFormField>

          <LazyUFormField label="Post Logout Redirect URIs">
            <LazyUTextarea
              v-model="form.postLogoutRedirectUrisText"
              :rows="3"
              placeholder="https://app.example.com/signed-out"
            />
          </LazyUFormField>

          <LazyUCheckbox
            v-if="isEditing && form.platform === 'Web'"
            v-model="form.rotateClientSecret"
            label="Rotate client secret"
          />

          <div class="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
            <UButton
              variant="ghost"
              class="w-full sm:w-auto"
              @click="isEditorOpen = false"
            >
              Close
            </UButton>
            <UButton
              type="submit"
              class="w-full sm:w-auto"
              :loading="isSubmitting"
            >
              {{ isEditing ? 'Update' : 'Create' }}
            </UButton>
          </div>
        </form>
      </UCard>
    </template>
  </UModal>

  <UModal :open="!!pendingDeleteApplication">
    <template #content>
      <UCard>
        <template #header>
          <h2 class="text-base font-semibold">
            Delete OAuth Application
          </h2>
        </template>

        <p class="text-sm text-(--ui-text-muted)">
          Delete {{ pendingDeleteApplication?.displayName ?? pendingDeleteApplication?.clientId }}?
        </p>

        <template #footer>
          <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <UButton
              variant="ghost"
              class="w-full sm:w-auto"
              @click="pendingDeleteApplication = null"
            >
              Cancel
            </UButton>
            <UButton
              color="error"
              icon="i-lucide-trash-2"
              class="w-full sm:w-auto"
              :loading="deleteMutation.isPending.value"
              @click="confirmDelete"
            >
              Delete
            </UButton>
          </div>
        </template>
      </UCard>
    </template>
  </UModal>
</template>
