<script setup lang="ts">
import { Comark } from '@comark/vue'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

definePageMeta({
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const queryClient = useQueryClient()
const toast = useToast()

const slug = computed(() => (route.params.slug as string | undefined) ?? '')

const { data: workshop, isLoading: isLoadingWorkshop, error: workshopError } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint(slug)
const { data: user, isSuccess: authResolved, isError: authErrored } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const workshopId = computed(() => workshop.value?.id ?? '')

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint(
  workshopId,
  { query: { enabled: computed(() => !!workshopId.value && !!user.value) } },
)

const { data: questionsData, isLoading: isLoadingQuestions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint(
  workshopId,
  { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } },
)

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint(
  workshopId,
  { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } },
)

const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint()

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

  const sameDay = start.toDateString() === end.toDateString()

  const dateFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    timeZone: 'Asia/Singapore',
  })

  const dateWithYearFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'Asia/Singapore',
  })

  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    timeZone: 'Asia/Singapore',
  })

  return {
    dateLabel: sameDay
      ? dateWithYearFormatter.format(start)
      : `${dateFormatter.format(start)} to ${dateWithYearFormatter.format(end)}`,
    timeLabel: `${timeFormatter.format(start)} to ${timeFormatter.format(end)} SGT`,
  }
})

async function startRegistration() {
  if (!workshopId.value)
    return

  try {
    await joinMutation.mutateAsync({ standaloneWorkshopId: workshopId.value })
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
      title: 'Registration started',
      description: 'Your registration form is ready.',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to start standalone workshop registration', error)
    toast.add({
      title: 'Unable to start registration',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

const loginUrl = computed(() =>
  `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`,
)
</script>

<template>
  <div class="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(0,0,0,0.03),_transparent_28%),linear-gradient(180deg,_#fcfcfb_0%,_#f8f8f6_100%)] text-(--ui-text)">
    <div class="mx-auto flex min-h-screen w-full max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between border-b border-black/8 pb-4">
        <NuxtLink
          to="/"
          class="text-sm font-medium tracking-[0.18em] text-black/65 uppercase"
        >
          GeeksHacking
        </NuxtLink>
        <div class="flex items-center gap-3">
          <UBadge
            v-if="workshop?.isPublished"
            size="sm"
            color="success"
            variant="subtle"
          >
            Open for registration
          </UBadge>
          <UButton
            v-if="!user && (authResolved || authErrored)"
            :to="loginUrl"
            external
            size="sm"
            variant="outline"
            color="neutral"
            icon="i-lucide-github"
          >
            Sign in
          </UButton>
        </div>
      </div>

      <div
        v-if="isLoadingWorkshop"
        class="flex flex-1 items-center justify-center"
      >
        <div class="space-y-3 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="mx-auto size-8 animate-spin text-black/50"
          />
          <p class="text-sm text-black/60">
            Loading workshop details...
          </p>
        </div>
      </div>

      <div
        v-else-if="workshopError || !workshop"
        class="flex flex-1 items-center justify-center"
      >
        <UCard class="max-w-xl">
          <div class="space-y-3 text-center">
            <p class="text-lg font-semibold text-(--ui-text-highlighted)">
              Workshop not found
            </p>
            <p class="text-sm text-(--ui-text-muted)">
              This workshop may no longer be public, or the link may be incorrect.
            </p>
          </div>
        </UCard>
      </div>

      <div
        v-else
        class="flex flex-1 justify-center py-6 lg:py-8"
      >
        <div class="w-full max-w-6xl space-y-5 lg:space-y-6">
          <section class="reveal-up reveal-delay-1 overflow-hidden rounded-[2rem] border border-black/6 bg-white/92 shadow-[0_22px_70px_-42px_rgba(0,0,0,0.16)] backdrop-blur transition-transform duration-300 ease-out hover:-translate-y-0.5 hover:shadow-[0_28px_80px_-44px_rgba(0,0,0,0.18)]">
            <div class="grid gap-5 p-5 sm:p-7 lg:gap-6 lg:p-10">
              <div class="flex flex-wrap items-center gap-3">
                <UBadge
                  color="warning"
                  variant="soft"
                  size="sm"
                >
                  Workshop
                </UBadge>
                <UBadge
                  v-if="registrationState === 'registered'"
                  color="success"
                  variant="subtle"
                  size="sm"
                >
                  Registered
                </UBadge>
                <UBadge
                  v-else-if="registrationState === 'incomplete'"
                  color="warning"
                  variant="subtle"
                  size="sm"
                >
                  Registration incomplete
                </UBadge>
              </div>

              <div class="space-y-4">
                <h1 class="max-w-4xl text-3xl font-semibold tracking-tight text-(--ui-text-highlighted) sm:text-5xl">
                  {{ workshop.title }}
                </h1>
                <div class="max-w-3xl text-sm leading-7 text-black/68 sm:text-base lg:text-lg">
                  <Suspense>
                    <Comark
                      :markdown="workshop.description"
                      :options="{ autoClose: true, autoUnwrap: true }"
                      class="workshop-markdown"
                    />
                  </Suspense>
                </div>
              </div>
            </div>
          </section>

          <div class="grid gap-6 lg:grid-cols-[minmax(0,0.82fr)_minmax(22rem,0.64fr)] lg:gap-8">
            <div class="space-y-5 lg:space-y-6">
              <div class="grid gap-3 sm:grid-cols-2">
                <UCard
                  :ui="{ body: 'p-4' }"
                  class="reveal-up reveal-delay-2 border-black/6 bg-white/88 transition-all duration-300 ease-out hover:-translate-y-0.5 hover:bg-white"
                >
                  <p class="text-xs font-medium tracking-[0.14em] text-black/50 uppercase">
                    Date / time
                  </p>
                  <p class="mt-2 text-sm font-semibold leading-6 text-(--ui-text-highlighted)">
                    {{ formattedDateTime.dateLabel }}
                  </p>
                  <p class="mt-1 text-sm leading-6 text-black/62">
                    {{ formattedDateTime.timeLabel }}
                  </p>
                </UCard>

                <UCard
                  :ui="{ body: 'p-4' }"
                  class="reveal-up reveal-delay-2 border-black/6 bg-white/88 transition-all duration-300 ease-out hover:-translate-y-0.5 hover:bg-white"
                >
                  <p class="text-xs font-medium tracking-[0.14em] text-black/50 uppercase">
                    Location
                  </p>
                  <p class="mt-2 text-sm font-medium leading-6 text-(--ui-text-highlighted)">
                    {{ workshop.location || 'To be announced' }}
                  </p>
                </UCard>
              </div>

              <div class="flex flex-wrap gap-3">
                <UButton
                  v-if="workshop.homepageUri"
                  :to="workshop.homepageUri"
                  external
                  target="_blank"
                  size="lg"
                  color="neutral"
                  variant="outline"
                  icon="i-lucide-arrow-up-right"
                >
                  Visit event site
                </UButton>
                <div class="rounded-2xl border border-black/6 bg-white/78 px-4 py-3 text-sm leading-6 text-black/62 transition-colors duration-300 ease-out hover:bg-white/92">
                  Sign in with GitHub to complete registration and update your answers later.
                </div>
              </div>

              <section
                v-if="registrationState === 'registered' || registrationState === 'incomplete'"
                class="reveal-up reveal-delay-3 rounded-[1.75rem] border border-black/6 bg-white/90 px-5 py-4 shadow-[0_18px_44px_-38px_rgba(0,0,0,0.14)] transition-transform duration-300 ease-out hover:-translate-y-0.5"
              >
                <UAlert
                  v-if="registrationState === 'registered'"
                  color="success"
                  variant="subtle"
                  icon="i-lucide-check-circle-2"
                  title="Registration complete"
                  description="Your required answers are saved. You can still update them below."
                />

                <UAlert
                  v-else
                  color="warning"
                  variant="subtle"
                  icon="i-lucide-circle-alert"
                  title="Registration in progress"
                  description="Complete the required questions below to finish your workshop signup."
                />
              </section>
            </div>

            <aside class="reveal-up reveal-delay-3 lg:sticky lg:top-6 lg:self-start">
              <UCard
                :ui="{ body: 'p-5 sm:p-6 lg:p-7' }"
                class="border-black/6 bg-white/94 shadow-[0_18px_48px_-36px_rgba(0,0,0,0.16)] transition-transform duration-300 ease-out hover:-translate-y-0.5 hover:shadow-[0_24px_60px_-36px_rgba(0,0,0,0.18)]"
              >
                <div class="space-y-4 sm:space-y-5">
                  <div class="space-y-2">
                    <p class="text-xs font-medium tracking-[0.16em] text-black/50 uppercase">
                      Registration
                    </p>
                    <h2 class="text-2xl font-semibold text-(--ui-text-highlighted)">
                      Reserve your spot
                    </h2>
                    <p class="text-sm leading-6 text-black/68">
                      Complete the signup form with your GitHub account. Your progress is saved when you submit.
                    </p>
                  </div>

                  <div
                    v-if="registrationState === 'checking-auth' || registrationState === 'checking-status' || registrationState === 'loading-registration'"
                    class="rounded-2xl border border-black/6 bg-white/84 p-5 text-sm text-black/62 transition-all duration-300 ease-out"
                  >
                    <div class="flex items-center gap-3">
                      <UIcon
                        name="i-lucide-loader-circle"
                        class="size-5 animate-spin text-black/45"
                      />
                      <span>Preparing your registration experience...</span>
                    </div>
                  </div>

                  <div
                    v-else-if="registrationState === 'signed-out'"
                    class="space-y-4 rounded-2xl border border-black/6 bg-white/84 p-5"
                  >
                    <p class="text-sm leading-6 text-black/70">
                      You can review the workshop details without signing in. To register, sign in first and we’ll bring you straight back here.
                    </p>
                    <UButton
                      :to="loginUrl"
                      external
                      block
                      size="lg"
                      icon="i-lucide-github"
                    >
                      Sign in to register
                    </UButton>
                  </div>

                  <div
                    v-else-if="registrationState === 'ready-to-join'"
                    class="space-y-4 rounded-2xl border border-black/6 bg-white/84 p-5"
                  >
                    <p class="text-sm leading-6 text-black/70">
                      You’re signed in. Start registration to unlock the workshop questions and save your spot.
                    </p>
                    <UButton
                      block
                      size="lg"
                      icon="i-lucide-ticket"
                      :loading="joinMutation.isPending.value"
                      @click="startRegistration"
                    >
                      Start registration
                    </UButton>
                  </div>

                  <div
                    v-else
                    class="space-y-5"
                  >
                    <Transition
                      v-if="questionsData?.categories?.length"
                      enter-active-class="transition duration-400 ease-out"
                      enter-from-class="translate-y-2 opacity-0"
                      enter-to-class="translate-y-0 opacity-100"
                      leave-active-class="transition duration-200 ease-in"
                      leave-from-class="opacity-100"
                      leave-to-class="opacity-0"
                    >
                      <div class="rounded-[1.5rem] bg-transparent">
                        <WorkshopsRegistrationForm
                          :standalone-workshop-id="workshopId"
                          :workshop-title="workshop.title"
                          :questions="questionsData"
                        />
                      </div>
                    </Transition>

                    <div
                      v-else
                      class="rounded-2xl border border-dashed border-black/10 bg-white/82 p-5 text-sm text-black/64"
                    >
                      No registration questions have been configured for this workshop yet.
                    </div>
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
@media (prefers-reduced-motion: no-preference) {
  .reveal-up {
    animation: reveal-up 560ms cubic-bezier(0.22, 1, 0.36, 1) both;
  }

  .reveal-delay-1 {
    animation-delay: 60ms;
  }

  .reveal-delay-2 {
    animation-delay: 140ms;
  }

  .reveal-delay-3 {
    animation-delay: 220ms;
  }
}

@keyframes reveal-up {
  from {
    opacity: 0;
    transform: translateY(18px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

:deep(.workshop-markdown) {
  display: grid;
  gap: 0.9rem;
}

:deep(.workshop-markdown p) {
  margin: 0;
}

:deep(.workshop-markdown h1),
:deep(.workshop-markdown h2),
:deep(.workshop-markdown h3),
:deep(.workshop-markdown h4) {
  margin: 0;
  color: color-mix(in oklab, currentColor 88%, black 12%);
  font-weight: 600;
  letter-spacing: -0.02em;
}

:deep(.workshop-markdown h1) {
  font-size: 1.45em;
}

:deep(.workshop-markdown h2) {
  font-size: 1.2em;
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
  color: color-mix(in oklab, currentColor 92%, black 8%);
  font-weight: 600;
}

:deep(.workshop-markdown code) {
  border: 1px solid rgb(0 0 0 / 0.08);
  border-radius: 0.65rem;
  background: rgb(255 255 255 / 0.82);
  padding: 0.1rem 0.45rem;
  font-size: 0.92em;
}
</style>
