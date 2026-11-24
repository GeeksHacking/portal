<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsWithdrawEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { githubLoginUrl } from '~/utils/login-url'

/**
 * "Your registration" for a workshop, inside the portal shell: confirmation, when and where,
 * add-to-calendar, the saved answers and a way to withdraw. Only for people who finished
 * registering; everyone else is sent to the workshop page (or to sign in).
 */
definePageMeta({
  auth: false,
  layout: 'portal',
})

const route = useRoute()
const toast = useGToast()
const queryClient = useQueryClient()
const config = useRuntimeConfig()

const slug = computed(() => (route.params.slug as string | undefined) ?? '')
const registrationPath = computed(() => `/workshops/${slug.value}`)
const loginUrl = computed(() => githubLoginUrl(config.public.api, route.fullPath))

const { data: workshop, isLoading: isLoadingWorkshop, error: workshopError } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint({ path: computed(() => ({ standaloneWorkshopIdOrShortCode: slug.value })) })
const workshopId = computed(() => workshop.value?.id ?? '')

const { data: user, isLoading: isLoadingUser, isError: isAuthError } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && !!user.value) } })
const isRegistered = computed(() => statusData.value?.isRegistered === true)

const { data: questionsData, isLoading: isLoadingQuestions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && isRegistered.value) } })
const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && isRegistered.value) } })

useHead(() => ({
  title: workshop.value?.title ? `Your registration · ${workshop.value.title}` : 'Your registration',
}))

const isLoading = computed(() =>
  isLoadingWorkshop.value
  || isLoadingUser.value
  || isLoadingStatus.value
  || isLoadingQuestions.value
  || isLoadingSubmissions.value,
)

// Only for finished registrations: sign in first, and anyone not (fully) registered goes to the workshop page
const isLeaving = ref(false)
watchEffect(() => {
  if (isLeaving.value || isLoadingWorkshop.value || !workshop.value)
    return

  if (!isLoadingUser.value && (!user.value || isAuthError.value)) {
    navigateTo(loginUrl.value, { external: true })
    return
  }

  if (!isLoadingStatus.value && statusData.value && !statusData.value.isRegistered) {
    navigateTo(registrationPath.value, { replace: true })
    return
  }

  if (!isLoadingSubmissions.value && submissionsData.value && (submissionsData.value.requiredQuestionsRemaining ?? 0) > 0)
    navigateTo(registrationPath.value, { replace: true })
})

const formattedDateTime = computed(() => formatWorkshopWhen(workshop.value?.startTime, workshop.value?.endTime))
const registeredAtLabel = computed(() => formatWorkshopRegisteredAt(statusData.value?.registeredAt))
const { answerGroups, savedAnswersCount, totalQuestionsCount } = useWorkshopAnswers(questionsData, submissionsData)

// ---- withdraw ---------------------------------------------------------------------------------
const withdrawMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsWithdrawEndpoint()
const isWithdrawModalOpen = ref(false)
const withdrawError = ref('')

const canWithdraw = computed(() =>
  !!workshop.value?.endTime && new Date(workshop.value.endTime).getTime() > Date.now(),
)

watch(isWithdrawModalOpen, (open) => {
  if (open)
    withdrawError.value = ''
})

async function withdrawFromWorkshop() {
  if (!workshopId.value)
    return

  withdrawError.value = ''
  try {
    await withdrawMutation.mutateAsync({ path: { standaloneWorkshopId: workshopId.value } })
    isLeaving.value = true
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpointQueryKey({ path: { standaloneWorkshopId: workshopId.value } }) }),
      queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpointQueryKey({ path: { standaloneWorkshopId: workshopId.value } }) }),
    ])
    isWithdrawModalOpen.value = false
    toast.add({ title: 'You\'ve withdrawn', description: workshop.value?.title ? `You're no longer registered for ${workshop.value.title}.` : undefined, tone: 'ok' })
    await navigateTo(registrationPath.value, { replace: true })
  }
  catch (error) {
    console.error('Failed to withdraw from workshop', error)
    isLeaving.value = false
    withdrawError.value = getApiErrorMessage(error, 'We couldn\'t withdraw you. Try again in a moment.')
  }
}

// ---- calendar ---------------------------------------------------------------------------------
function formatCalendarDate(value: string | undefined) {
  if (!value)
    return ''

  return new Date(value).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}Z$/, 'Z')
}

function escapeCalendarText(value: string | null | undefined) {
  return (value ?? '')
    .replace(/\\/g, '\\\\')
    .replace(/\n/g, '\\n')
    .replace(/,/g, '\\,')
    .replace(/;/g, '\\;')
}

const calendarEvent = computed(() => {
  if (!workshop.value?.startTime || !workshop.value?.endTime)
    return null

  const title = workshop.value.title ?? 'Workshop'
  const location = workshop.value.location ?? ''
  const detailsUrl = import.meta.client ? `${window.location.origin}${registrationPath.value}` : registrationPath.value

  return {
    title,
    description: `Registered for ${title}. Details: ${detailsUrl}`,
    location,
    start: formatCalendarDate(workshop.value.startTime),
    end: formatCalendarDate(workshop.value.endTime),
  }
})

const googleCalendarUrl = computed(() => {
  if (!calendarEvent.value)
    return ''

  const event = calendarEvent.value
  const params = new URLSearchParams({
    action: 'TEMPLATE',
    text: event.title,
    dates: `${event.start}/${event.end}`,
    details: event.description,
    location: event.location,
  })

  return `https://calendar.google.com/calendar/render?${params.toString()}`
})

function downloadCalendarFile() {
  if (!calendarEvent.value || !import.meta.client)
    return

  const event = calendarEvent.value
  const content = [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//GeeksHacking//Portal//EN',
    'BEGIN:VEVENT',
    `UID:${workshopId.value || slug.value}@geekshacking.com`,
    `DTSTAMP:${formatCalendarDate(new Date().toISOString())}`,
    `DTSTART:${event.start}`,
    `DTEND:${event.end}`,
    `SUMMARY:${escapeCalendarText(event.title)}`,
    `DESCRIPTION:${escapeCalendarText(event.description)}`,
    `LOCATION:${escapeCalendarText(event.location)}`,
    'END:VEVENT',
    'END:VCALENDAR',
  ].join('\r\n')

  const url = URL.createObjectURL(new Blob([content], { type: 'text/calendar;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `${slug.value || 'workshop'}.ics`
  link.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <GPanel id="workshop-registration">
    <template #header>
      <GPanelHeader
        title="Your registration"
        as="p"
      >
        <template #right>
          <WorkshopsCheckinQr
            v-if="workshop && isRegistered"
            :workshop-id="workshopId"
            :user-id="user?.id ?? ''"
            :registration-id="statusData?.registrationId"
            :workshop-title="workshop.title"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="wr">
        <GLoading
          v-if="isLoading"
          label="Loading your registration…"
        />

        <GEmptyState
          v-else-if="workshopError || !workshop"
          icon="material-symbols:search-off-rounded"
          title="Workshop not found"
          description="It may no longer be public, or the link may be wrong."
        >
          <template #actions>
            <GButton
              to="/dash"
              variant="secondary"
              icon="material-symbols:explore-rounded"
              label="Explore events"
            />
          </template>
        </GEmptyState>

        <template v-else-if="isRegistered">
          <!-- confirmation -->
          <header class="wr-head">
            <div class="flex flex-wrap items-center gap-2">
              <GLabelStrip label="Workshop" />
              <GBadge
                tone="ok"
                icon="material-symbols:check-circle-rounded"
                label="You're registered"
              />
            </div>
            <h1 class="wr-head__title">
              {{ workshop.title }}
            </h1>
            <p class="wr-head__lead">
              Your registration is complete. If something below looks wrong, contact the organisers.
            </p>
          </header>

          <!-- when, where, calendar -->
          <section
            class="wr-card"
            aria-labelledby="wr-details-title"
          >
            <h2
              id="wr-details-title"
              class="wr-card__title"
            >
              Details
            </h2>
            <dl class="wr-facts">
              <div>
                <dt>When</dt>
                <dd>
                  {{ formattedDateTime.dateLabel }}
                  <span class="wr-facts__sub">{{ formattedDateTime.timeLabel }}</span>
                </dd>
              </div>
              <div>
                <dt>Where</dt>
                <dd>{{ workshop.location || 'To be announced' }}</dd>
              </div>
              <div>
                <dt>Registered</dt>
                <dd>{{ registeredAtLabel }}</dd>
              </div>
              <div>
                <dt>Answers saved</dt>
                <dd>{{ savedAnswersCount }} of {{ totalQuestionsCount }}</dd>
              </div>
            </dl>

            <div
              v-if="calendarEvent"
              class="wr-calendar"
            >
              <p class="wr-calendar__text">
                Add it to your calendar so you have the time and place handy.
              </p>
              <div class="flex flex-wrap gap-2">
                <GButton
                  :href="googleCalendarUrl"
                  target="_blank"
                  size="sm"
                  icon="material-symbols:calendar-add-on-rounded"
                  label="Google Calendar"
                  aria-label="Add to Google Calendar (opens in a new tab)"
                />
                <GButton
                  size="sm"
                  icon="material-symbols:download-rounded"
                  label="Download .ics"
                  aria-label="Download a calendar file (.ics)"
                  @click="downloadCalendarFile"
                />
              </div>
            </div>

            <div class="wr-actions">
              <GButton
                :to="registrationPath"
                variant="ghost"
                icon="material-symbols:arrow-back-rounded"
                label="Workshop details"
              />
              <GButton
                v-if="workshop.homepageUri"
                :href="workshop.homepageUri"
                target="_blank"
                variant="ghost"
                trailing-icon="material-symbols:open-in-new-rounded"
                label="Event site"
                aria-label="Event site (opens in a new tab)"
              />
            </div>
          </section>

          <!-- answers -->
          <section
            class="wr-section"
            aria-labelledby="wr-answers-title"
          >
            <h2
              id="wr-answers-title"
              class="wr-card__title"
            >
              Your answers
            </h2>
            <WorkshopsAnswerList :groups="answerGroups" />
          </section>

          <!-- withdraw -->
          <section
            v-if="canWithdraw"
            class="wr-withdraw"
            aria-labelledby="wr-withdraw-title"
          >
            <div class="wr-withdraw__body">
              <h2
                id="wr-withdraw-title"
                class="wr-withdraw__title"
              >
                Can't make it?
              </h2>
              <p class="wr-withdraw__text">
                Withdraw to free up your spot for someone else. You can register again later if there's still room.
              </p>
            </div>
            <GButton
              variant="danger"
              icon="material-symbols:person-remove-rounded"
              label="Withdraw"
              @click="isWithdrawModalOpen = true"
            />
          </section>
        </template>
      </div>
    </template>
  </GPanel>

  <GModal
    v-model:open="isWithdrawModalOpen"
    :title="`Withdraw from ${workshop?.title ?? 'this workshop'}?`"
    description="Your registration and answers are cancelled. You can register again later if there's still room."
    size="sm"
  >
    <GAlert
      v-if="withdrawError"
      tone="err"
      title="Not withdrawn"
      :description="withdrawError"
    />
    <template #footer>
      <GButton
        variant="ghost"
        label="Stay registered"
        @click="isWithdrawModalOpen = false"
      />
      <GButton
        variant="danger"
        label="Withdraw"
        :loading="withdrawMutation.isPending.value"
        loading-label="Withdrawing"
        @click="withdrawFromWorkshop"
      />
    </template>
  </GModal>
</template>

<style scoped>
.wr {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 56rem;
  margin: 0 auto;
}
.wr-head {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.wr-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
  overflow-wrap: anywhere;
}
.wr-head__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.wr-card {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
@media (min-width: 640px) {
  .wr-card { padding: 24px; }
}
.wr-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
}
.wr-section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.wr-facts {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px 24px;
  margin: 0;
}
@media (min-width: 640px) {
  .wr-facts { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
.wr-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.wr-facts dd {
  margin: 2px 0 0;
  font-weight: 600;
  overflow-wrap: anywhere;
}
.wr-facts__sub {
  display: block;
  font-size: 14px;
  font-weight: 400;
  color: var(--gh-text-muted);
}
.wr-calendar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px 16px;
  padding-top: 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.wr-calendar__text {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.wr-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin: -8px 0 -8px -12px;
}
.wr-withdraw {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px 24px;
  padding: 20px;
  border: var(--gh-border-width) dashed var(--gh-line);
  border-radius: var(--radius-gh-lg);
}
.wr-withdraw__body {
  flex: 1 1 18rem;
  min-width: 0;
}
.wr-withdraw__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.wr-withdraw__text {
  max-width: 60ch;
  margin: 4px 0 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
</style>
