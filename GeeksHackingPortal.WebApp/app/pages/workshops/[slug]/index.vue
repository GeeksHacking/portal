<script setup lang="ts">
import { Markdown } from '@comark/vue'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { useProfileName } from '~/composables/useProfileName'
import { getApiErrorMessage } from '~/utils/api-errors'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, isSameHackathonDay } from '~/utils/hackathon-date-time'

definePageMeta({
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const queryClient = useQueryClient()
const toast = useToast()

const slug = computed(() => (route.params.slug as string | undefined) ?? '')

const { data: workshop, isLoading: isLoadingWorkshop, error: workshopError } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint({ path: computed(() => ({ standaloneWorkshopIdOrShortCode: slug.value })) })
const { data: user, isSuccess: authResolved, isError: authErrored } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const workshopId = computed(() => workshop.value?.id ?? '')

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && !!user.value) } })

const { data: questionsData, isLoading: isLoadingQuestions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } })

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } })

const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint()

// The name lives on the user profile and is confirmed when joining, not in the workshop's registration answers
const { profileName, isProfileNameValid, saveProfileName, isSavingProfileName } = useProfileName(user)

useHead(() => ({
  title: workshop.value?.title ? `${workshop.value.title} - GeeksHacking` : 'Workshop - GeeksHacking',
}))

const registrationState = computed(() => {
  if (!authResolved.value && !authErrored.value)
    return 'checking-auth'
  if (!user.value)
    return 'signed-out'
  if (!workshopId.value)
    return 'loading'
  if (isLoadingStatus.value)
    return 'checking-status'
  if (!statusData.value?.isRegistered)
    return 'ready-to-join'
  if (isLoadingQuestions.value || isLoadingSubmissions.value)
    return 'loading-registration'
  if ((submissionsData.value?.requiredQuestionsRemaining ?? 0) === 0)
    return 'registered'
  return 'incomplete'
})

const registeredPath = computed(() => `/workshops/${slug.value}/registered`)

const hasEnded = computed(() => {
  if (!workshop.value?.endTime)
    return false
  return new Date(workshop.value.endTime).getTime() <= Date.now()
})

const isWorkshopFull = ref(false)

const capacityNote = computed(() => {
  const max = workshop.value?.maxParticipants
  if (!max || max <= 0)
    return null
  return `Limited to ${max} participants`
})

const journeyStepIndex = computed(() => {
  switch (registrationState.value) {
    case 'signed-out':
    case 'checking-auth':
      return 0
    case 'ready-to-join':
    case 'checking-status':
    case 'loading':
      return 1
    case 'incomplete':
    case 'loading-registration':
      return 2
    case 'registered':
      return 3
    default:
      return 0
  }
})

const journeySteps = [
  { key: 'sign-in', label: 'Sign in', shortLabel: 'Sign in' },
  { key: 'confirm', label: 'Confirm name', shortLabel: 'Name' },
  { key: 'questions', label: 'Answer questions', shortLabel: 'Questions' },
] as const

const panelCopy = computed(() => {
  if (hasEnded.value) {
    return {
      eyebrow: 'Registration closed',
      title: 'This workshop has ended',
      description: 'Registration is no longer available. Browse other GeeksHacking events instead.',
    }
  }

  if (isWorkshopFull.value && registrationState.value === 'ready-to-join') {
    return {
      eyebrow: 'Workshop full',
      title: 'No spots left',
      description: 'This workshop has reached capacity. Check Explore events for other sessions.',
    }
  }

  switch (registrationState.value) {
    case 'checking-auth':
    case 'checking-status':
    case 'loading':
    case 'loading-registration':
      return {
        eyebrow: 'Registration',
        title: 'Getting things ready',
        description: 'Checking your account and workshop status…',
      }
    case 'signed-out':
      return {
        eyebrow: 'Step 1 of 3',
        title: 'Sign in to register',
        description: 'Use GitHub to continue. We will bring you straight back to this workshop.',
      }
    case 'ready-to-join':
      return {
        eyebrow: 'Step 2 of 3',
        title: 'Confirm your name',
        description: 'Organizers use this name for check-in and communications. Then you can finish any questions.',
      }
    case 'incomplete':
      return {
        eyebrow: 'Step 3 of 3',
        title: 'Finish your registration',
        description: 'Answer the workshop questions below. After you submit, your spot is confirmed and answers are locked.',
      }
    case 'registered':
      return {
        eyebrow: 'You are in',
        title: 'Registration complete',
        description: 'Taking you to your confirmation…',
      }
    default:
      return {
        eyebrow: 'Registration',
        title: 'Register for this workshop',
        description: 'Follow the steps to secure your spot.',
      }
  }
})

const headerBadge = computed(() => {
  if (hasEnded.value)
    return { label: 'Ended', color: 'neutral' as const }
  if (isWorkshopFull.value && registrationState.value !== 'registered' && registrationState.value !== 'incomplete')
    return { label: 'Full', color: 'warning' as const }
  if (registrationState.value === 'registered')
    return { label: 'Registered', color: 'success' as const }
  if (registrationState.value === 'incomplete')
    return { label: 'Finish registration', color: 'warning' as const }
  if (workshop.value?.isPublished)
    return { label: 'Open for registration', color: 'success' as const }
  return { label: null, color: 'neutral' as const }
})

const showDecisionAside = computed(() =>
  registrationState.value !== 'registered' && registrationState.value !== 'incomplete',
)

const showInlineForm = computed(() => registrationState.value === 'incomplete')

const isPreparing = computed(() =>
  registrationState.value === 'checking-auth'
  || registrationState.value === 'checking-status'
  || registrationState.value === 'loading'
  || registrationState.value === 'loading-registration',
)

watch(registrationState, (state) => {
  if (state === 'registered' && slug.value) {
    navigateTo(registeredPath.value, { replace: true })
  }
})

function standaloneWorkshopStatusQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/status', params: { standaloneWorkshopId } }] as const
}

function standaloneWorkshopRegistrationQuestionsQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/registration/questions', params: { standaloneWorkshopId } }] as const
}

function standaloneWorkshopRegistrationSubmissionsQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/registration/submissions', params: { standaloneWorkshopId } }] as const
}

const formattedDateTime = computed(() => {
  if (!workshop.value?.startTime || !workshop.value?.endTime) {
    return {
      dateLabel: 'Date to be announced',
      timeLabel: 'Time to be announced',
    }
  }

  const start = new Date(workshop.value.startTime)
  const end = new Date(workshop.value.endTime)

  const sameDay = isSameHackathonDay(start, end)

  const dateFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    timeZone: HACKATHON_TIME_ZONE,
  })

  const dateWithYearFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: HACKATHON_TIME_ZONE,
  })

  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    timeZone: HACKATHON_TIME_ZONE,
  })

  return {
    dateLabel: sameDay
      ? dateWithYearFormatter.format(start)
      : `${dateFormatter.format(start)} to ${dateWithYearFormatter.format(end)}`,
    timeLabel: `${timeFormatter.format(start)} to ${timeFormatter.format(end)} ${HACKATHON_TIME_ZONE_LABEL}`,
  }
})

async function startRegistration() {
  if (!workshopId.value || !isProfileNameValid.value || hasEnded.value)
    return

  try {
    isWorkshopFull.value = false
    await saveProfileName()
    await joinMutation.mutateAsync({ path: { standaloneWorkshopId: workshopId.value } })
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopStatusQueryKey(workshopId.value),
      }),
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopRegistrationQuestionsQueryKey(workshopId.value),
      }),
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopRegistrationSubmissionsQueryKey(workshopId.value),
      }),
    ])
    toast.add({
      title: 'Name confirmed',
      description: 'Next: complete any workshop questions to finish signup.',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to start standalone workshop registration', error)
    const message = getApiErrorMessage(error, 'Please try again.')
    if (message.toLowerCase().includes('full')) {
      isWorkshopFull.value = true
      toast.add({
        title: 'Workshop is full',
        description: 'No spots are left for this session.',
        color: 'error',
      })
      return
    }
    toast.add({
      title: 'Unable to start registration',
      description: message,
      color: 'error',
    })
  }
}

async function onRegistrationSubmitted() {
  await navigateTo(registeredPath.value)
}

const loginUrl = computed(() =>
  `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`,
)
</script>

<template>
  <div class="min-h-screen bg-default text-default">
    <WorkshopsPageHeader
      :login-url="loginUrl"
      :show-sign-in="!user && (authResolved || authErrored)"
      :signed-in-as="user?.gitHubLogin"
      :badge-label="headerBadge.label"
      :badge-color="headerBadge.color"
    />

    <div class="mx-auto flex min-h-[calc(100vh-3.5rem)] w-full max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
      <div
        v-if="isLoadingWorkshop"
        class="flex flex-1 items-center justify-center"
      >
        <div class="w-full max-w-md space-y-4 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="mx-auto size-8 animate-spin text-primary"
          />
          <div class="space-y-1">
            <p class="text-sm font-medium text-default">
              Loading workshop details
            </p>
            <p class="text-sm text-muted">
              Pulling the latest schedule, location, and registration options…
            </p>
          </div>
        </div>
      </div>

      <div
        v-else-if="workshopError || !workshop"
        class="flex flex-1 items-center justify-center py-10"
      >
        <UCard class="w-full max-w-lg bg-elevated/40 shadow-none ring-0">
          <div class="space-y-4 text-center">
            <div class="mx-auto flex size-12 items-center justify-center rounded-full bg-elevated">
              <UIcon
                name="i-lucide-search-x"
                class="size-5 text-muted"
              />
            </div>
            <div class="space-y-1">
              <p class="text-lg font-semibold tracking-tight text-default">
                Workshop not found
              </p>
              <p class="text-sm leading-6 text-muted">
                This workshop may have ended, is no longer public, or the link may be incorrect.
              </p>
            </div>
            <div class="flex flex-wrap items-center justify-center gap-2 pt-1">
              <UButton
                to="/dash"
                icon="i-lucide-layout-grid"
                size="sm"
              >
                Explore events
              </UButton>
            </div>
          </div>
        </UCard>
      </div>

      <div
        v-else
        class="flex flex-1 justify-center"
      >
        <div class="w-full max-w-6xl space-y-6">
          <nav
            aria-label="Registration progress"
            class="rounded-xl bg-elevated/40 px-4 py-3 sm:px-5"
          >
            <ol class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <li
                v-for="(step, index) in journeySteps"
                :key="step.key"
                class="flex min-w-0 flex-1 items-center gap-3"
              >
                <div
                  class="flex size-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold"
                  :class="index < journeyStepIndex
                    ? 'bg-success text-white'
                    : index === journeyStepIndex
                      ? 'bg-primary text-white'
                      : 'bg-elevated text-muted'"
                >
                  <UIcon
                    v-if="index < journeyStepIndex"
                    name="i-lucide-check"
                    class="size-3.5"
                  />
                  <span v-else>{{ index + 1 }}</span>
                </div>
                <div class="min-w-0">
                  <p
                    class="truncate text-sm font-medium"
                    :class="index <= journeyStepIndex ? 'text-default' : 'text-muted'"
                  >
                    <span class="sm:hidden">{{ step.shortLabel }}</span>
                    <span class="hidden sm:inline">{{ step.label }}</span>
                  </p>
                </div>
                <div
                  v-if="index < journeySteps.length - 1"
                  class="ml-auto hidden h-px flex-1 bg-default sm:block"
                />
              </li>
            </ol>
          </nav>

          <section class="rounded-xl bg-elevated/50 p-5 sm:p-7 lg:p-8">
            <div class="space-y-5">
              <div class="flex flex-wrap items-center gap-2">
                <UBadge
                  color="info"
                  variant="subtle"
                  size="sm"
                  icon="i-lucide-brain-circuit"
                >
                  Workshop
                </UBadge>
                <UBadge
                  v-if="capacityNote"
                  color="neutral"
                  variant="outline"
                  size="sm"
                >
                  {{ capacityNote }}
                </UBadge>
              </div>

              <div class="max-w-4xl space-y-3">
                <h1 class="text-3xl font-semibold tracking-tight text-default sm:text-4xl lg:text-[2.75rem] lg:leading-tight">
                  {{ workshop.title }}
                </h1>
                <div class="text-sm leading-7 text-muted sm:text-base">
                  <Suspense>
                    <Markdown
                      :value="workshop.description"
                      :options="{ autoClose: true, autoUnwrap: true }"
                      class="workshop-markdown"
                    />
                  </Suspense>
                </div>
              </div>

              <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                <div class="rounded-lg bg-default/70 p-4">
                  <div class="flex items-start gap-3">
                    <div class="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-elevated">
                      <UIcon
                        name="i-lucide-calendar-days"
                        class="size-4 text-primary"
                      />
                    </div>
                    <div class="min-w-0">
                      <p class="text-xs font-medium tracking-[0.12em] text-muted uppercase">
                        Date
                      </p>
                      <p class="mt-1 text-sm font-semibold leading-6 text-default">
                        {{ formattedDateTime.dateLabel }}
                      </p>
                    </div>
                  </div>
                </div>

                <div class="rounded-lg bg-default/70 p-4">
                  <div class="flex items-start gap-3">
                    <div class="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-elevated">
                      <UIcon
                        name="i-lucide-clock-3"
                        class="size-4 text-primary"
                      />
                    </div>
                    <div class="min-w-0">
                      <p class="text-xs font-medium tracking-[0.12em] text-muted uppercase">
                        Time
                      </p>
                      <p class="mt-1 text-sm font-semibold leading-6 text-default">
                        {{ formattedDateTime.timeLabel }}
                      </p>
                    </div>
                  </div>
                </div>

                <div class="rounded-lg bg-default/70 p-4 sm:col-span-2 lg:col-span-1">
                  <div class="flex items-start gap-3">
                    <div class="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md bg-elevated">
                      <UIcon
                        name="i-lucide-map-pin"
                        class="size-4 text-primary"
                      />
                    </div>
                    <div class="min-w-0">
                      <p class="text-xs font-medium tracking-[0.12em] text-muted uppercase">
                        Location
                      </p>
                      <p class="mt-1 text-sm font-semibold leading-6 text-default">
                        {{ workshop.location || 'To be announced' }}
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              <div
                v-if="workshop.homepageUri"
                class="flex flex-wrap gap-2"
              >
                <UButton
                  :to="workshop.homepageUri"
                  external
                  target="_blank"
                  color="neutral"
                  variant="outline"
                  icon="i-lucide-external-link"
                  size="sm"
                >
                  Visit event site
                </UButton>
              </div>
            </div>
          </section>

          <div
            class="grid gap-6 lg:gap-8"
            :class="showDecisionAside ? 'lg:grid-cols-[minmax(0,1fr)_minmax(20rem,24rem)]' : 'lg:grid-cols-1'"
          >
            <div class="space-y-5">
              <section
                v-if="showInlineForm"
                class="space-y-4"
              >
                <div class="rounded-xl bg-elevated/50 p-5 sm:p-6">
                  <div class="space-y-1">
                    <p class="text-xs font-medium tracking-[0.14em] text-muted uppercase">
                      {{ panelCopy.eyebrow }}
                    </p>
                    <h2 class="text-xl font-semibold tracking-tight text-default sm:text-2xl">
                      {{ panelCopy.title }}
                    </h2>
                    <p class="text-sm leading-6 text-muted">
                      {{ panelCopy.description }}
                    </p>
                  </div>
                </div>

                <div
                  v-if="questionsData?.categories?.length"
                  class="rounded-xl bg-elevated/40 p-4 sm:p-6"
                >
                  <LazyWorkshopsRegistrationForm
                    :standalone-workshop-id="workshopId"
                    :workshop-title="workshop.title"
                    :questions="questionsData"
                    @submitted="onRegistrationSubmitted"
                  />
                </div>

                <UAlert
                  v-else
                  color="warning"
                  variant="soft"
                  icon="i-lucide-circle-alert"
                  title="Questions are not ready yet"
                  description="You have started registration, but organizers have not published questions. Check back shortly or contact the organizer."
                />
              </section>

              <UAlert
                v-else-if="registrationState === 'registered'"
                color="success"
                variant="subtle"
                icon="i-lucide-loader-circle"
                title="Registration complete"
                description="Opening your confirmation page…"
              />
            </div>

            <aside
              v-if="showDecisionAside"
              class="lg:sticky lg:top-20 lg:self-start"
            >
              <UCard
                :ui="{ body: 'p-5 sm:p-6', header: 'px-5 py-4 sm:px-6' }"
                class="bg-elevated/50 shadow-none ring-0"
              >
                <template #header>
                  <div class="space-y-1">
                    <p class="text-xs font-medium tracking-[0.14em] text-muted uppercase">
                      {{ panelCopy.eyebrow }}
                    </p>
                    <h2 class="text-xl font-semibold tracking-tight text-default">
                      {{ panelCopy.title }}
                    </h2>
                    <p class="text-sm leading-6 text-muted">
                      {{ panelCopy.description }}
                    </p>
                  </div>
                </template>

                <div class="space-y-4">
                  <div
                    v-if="isPreparing"
                    class="rounded-lg bg-default/70 p-4 text-sm text-muted"
                  >
                    <div class="flex items-center gap-3">
                      <UIcon
                        name="i-lucide-loader-circle"
                        class="size-5 animate-spin text-primary"
                      />
                      <span>Preparing your next step…</span>
                    </div>
                  </div>

                  <div
                    v-else-if="hasEnded || (isWorkshopFull && registrationState === 'ready-to-join')"
                    class="space-y-3"
                  >
                    <UButton
                      to="/dash"
                      block
                      size="lg"
                      icon="i-lucide-layout-grid"
                    >
                      Explore other events
                    </UButton>
                  </div>

                  <div
                    v-else-if="registrationState === 'signed-out'"
                    class="space-y-4"
                  >
                    <ul class="space-y-2 text-sm leading-6 text-muted">
                      <li class="flex gap-2">
                        <UIcon
                          name="i-lucide-check"
                          class="mt-1 size-4 shrink-0 text-success"
                        />
                        <span>Browse details freely — signup needs GitHub only at the end.</span>
                      </li>
                      <li class="flex gap-2">
                        <UIcon
                          name="i-lucide-check"
                          class="mt-1 size-4 shrink-0 text-success"
                        />
                        <span>After sign-in you confirm your name, then answer any questions.</span>
                      </li>
                    </ul>
                    <UButton
                      :to="loginUrl"
                      external
                      block
                      size="lg"
                      icon="i-lucide-github"
                    >
                      Sign in to register
                    </UButton>
                    <p class="text-center text-xs text-muted">
                      Already registered?
                      <NuxtLink
                        :to="loginUrl"
                        external
                        class="font-medium text-default underline-offset-2 hover:underline"
                      >
                        Sign in
                      </NuxtLink>
                      to view your confirmation.
                    </p>
                  </div>

                  <div
                    v-else-if="registrationState === 'ready-to-join'"
                    class="space-y-4"
                  >
                    <div class="grid gap-3">
                      <UFormField
                        label="First name"
                        required
                      >
                        <UInput
                          v-model="profileName.firstName"
                          autocomplete="given-name"
                          placeholder="Ada"
                          class="w-full"
                        />
                      </UFormField>
                      <UFormField
                        label="Last name"
                        required
                      >
                        <UInput
                          v-model="profileName.lastName"
                          autocomplete="family-name"
                          placeholder="Lovelace"
                          class="w-full"
                        />
                      </UFormField>
                    </div>
                    <p class="text-xs leading-5 text-muted">
                      This updates your GeeksHacking profile name everywhere — hackathons and workshops.
                    </p>
                    <UButton
                      block
                      size="lg"
                      icon="i-lucide-arrow-right"
                      trailing
                      :loading="joinMutation.isPending.value || isSavingProfileName"
                      :disabled="!isProfileNameValid"
                      @click="startRegistration"
                    >
                      Continue to questions
                    </UButton>
                  </div>
                </div>
              </UCard>
            </aside>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<style scoped>
:deep(.workshop-markdown) {
  display: grid;
  gap: 0.85rem;
}

:deep(.workshop-markdown p) {
  margin: 0;
}

:deep(.workshop-markdown h1),
:deep(.workshop-markdown h2),
:deep(.workshop-markdown h3),
:deep(.workshop-markdown h4) {
  margin: 0;
  color: var(--ui-text-highlighted);
  font-weight: 600;
  letter-spacing: -0.02em;
}

:deep(.workshop-markdown h1) {
  font-size: 1.35em;
}

:deep(.workshop-markdown h2) {
  font-size: 1.15em;
}

:deep(.workshop-markdown h3),
:deep(.workshop-markdown h4) {
  font-size: 1.05em;
}

:deep(.workshop-markdown ul),
:deep(.workshop-markdown ol) {
  margin: 0;
  padding-left: 1.25rem;
}

:deep(.workshop-markdown li + li) {
  margin-top: 0.35rem;
}

:deep(.workshop-markdown a) {
  color: inherit;
  text-decoration: underline;
  text-decoration-color: color-mix(in oklab, currentColor 28%, transparent);
  text-underline-offset: 0.18em;
}

:deep(.workshop-markdown strong) {
  color: var(--ui-text-highlighted);
  font-weight: 600;
}

:deep(.workshop-markdown code) {
  border: 1px solid var(--ui-border);
  border-radius: 0.4rem;
  background: var(--ui-bg-elevated);
  padding: 0.1rem 0.4rem;
  font-size: 0.92em;
}
</style>
