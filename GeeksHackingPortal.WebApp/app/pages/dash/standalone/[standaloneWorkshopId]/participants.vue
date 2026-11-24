<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListParticipantItem,
} from '@geekshacking/portal-sdk'
import type { GTabItem } from '~/components/g/Tabs.vue'
import {
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsWithdrawEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { formatOrganizerDateTime, waitedFor } from '~/utils/participant-review'

/**
 * Workshop participants: everyone who registered, with search and Registered / Withdrawn
 * filters. Pick someone to see their answers and check-ins, and withdraw them if needed.
 */
type ParticipantItem = GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListParticipantItem
type FilterStatus = 'all' | 'active' | 'withdrawn'

const route = useRoute()
const toast = useGToast()
const queryClient = useQueryClient()

const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')
const searchQuery = ref('')
const activeFilter = ref<FilterStatus>('all')
const selectedUserId = ref<string | null>(null)

const { data: eventsData } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const {
  data: participantsData,
  isLoading,
  error: participantsError,
} = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: standaloneWorkshopId.value })) })
const withdrawMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsWithdrawEndpoint()

const event = computed(() => eventsData.value?.standaloneWorkshops?.find(item => item.id === standaloneWorkshopId.value))
const participants = computed<ParticipantItem[]>(() => participantsData.value?.participants ?? [])

const { data: participantDetail, isLoading: isLoadingDetail } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint(
  { path: computed(() => ({ standaloneWorkshopId: standaloneWorkshopId.value, userId: selectedUserId.value ?? '' })) },
  { query: { enabled: computed(() => !!standaloneWorkshopId.value && !!selectedUserId.value) } },
)

const counts = computed(() => ({
  all: participants.value.length,
  active: participants.value.filter(p => !p.withdrawnAt).length,
  withdrawn: participants.value.filter(p => !!p.withdrawnAt).length,
  answered: participants.value.filter(p => (p.registrationSubmissions?.length ?? 0) > 0).length,
}))

const filterTabs = computed<GTabItem[]>(() => [
  { value: 'all', label: `All · ${counts.value.all}` },
  { value: 'active', label: `Registered · ${counts.value.active}` },
  { value: 'withdrawn', label: `Withdrawn · ${counts.value.withdrawn}` },
])
const filterModel = computed({
  get: () => activeFilter.value,
  set: (value: string) => { activeFilter.value = value as FilterStatus },
})

const visibleParticipants = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  return participants.value
    .filter((p) => {
      if (activeFilter.value === 'active')
        return !p.withdrawnAt
      if (activeFilter.value === 'withdrawn')
        return !!p.withdrawnAt
      return true
    })
    .filter((p) => {
      if (!query)
        return true
      if ([p.name, p.email, p.status, p.userId].filter(Boolean).join(' ').toLowerCase().includes(query))
        return true
      return (p.registrationSubmissions ?? []).some(s =>
        [s.questionText, s.value, s.followUpValue].filter(Boolean).join(' ').toLowerCase().includes(query))
    })
    .sort((a, b) => new Date(b.registeredAt ?? 0).getTime() - new Date(a.registeredAt ?? 0).getTime())
})

function displayName(p: { name?: string, email?: string, userId?: string } | null | undefined) {
  return p?.name || p?.email || 'Participant'
}

// ---- selection ----
const detailOpen = computed({
  get: () => !!selectedUserId.value,
  set: (value) => {
    if (!value)
      selectedUserId.value = null
  },
})
// list data first (instant), the detail call adds the check-ins
const selected = computed(() => participants.value.find(p => p.userId === selectedUserId.value) ?? null)
const selectedName = computed(() => displayName(participantDetail.value ?? selected.value))
const selectedAnswers = computed(() => participantDetail.value?.registrationSubmissions ?? selected.value?.registrationSubmissions ?? [])
const checkIns = computed(() => [...(participantDetail.value?.venueCheckIns ?? [])].sort((a, b) =>
  new Date(b.checkInTime ?? 0).getTime() - new Date(a.checkInTime ?? 0).getTime()))

const confirmingWithdraw = ref(false)
const withdrawError = ref('')

watch(selectedUserId, () => {
  confirmingWithdraw.value = false
  withdrawError.value = ''
})

function select(userId: string | undefined) {
  if (userId)
    selectedUserId.value = userId
}

async function withdraw() {
  const participant = selected.value
  if (!standaloneWorkshopId.value || !participant?.userId || participant.withdrawnAt)
    return
  withdrawError.value = ''
  try {
    await withdrawMutation.mutateAsync({
      path: { standaloneWorkshopId: standaloneWorkshopId.value, userId: participant.userId },
    })
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpointQueryKey({ path: { standaloneWorkshopId: standaloneWorkshopId.value } }),
      }),
      queryClient.invalidateQueries({
        queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpointQueryKey({ path: { standaloneWorkshopId: standaloneWorkshopId.value, userId: participant.userId } }),
      }),
    ])
    confirmingWithdraw.value = false
    toast.add({ tone: 'ok', title: `Withdrew ${displayName(participant)}` })
  }
  catch (error) {
    withdrawError.value = getApiErrorMessage(error, 'They weren\'t withdrawn. Try again.')
  }
}
</script>

<template>
  <GPanel id="standalone-participants">
    <template #header>
      <GPanelHeader title="Participants" />
    </template>

    <template #body>
      <div class="wp">
        <div class="wp-head">
          <div class="wp-head__text">
            <GLabelStrip
              label="Workshop"
              size="sm"
            />
            <p class="wp-head__title">
              {{ event?.title ?? 'Workshop' }}
            </p>
            <p
              v-if="participants.length"
              class="wp-muted"
            >
              {{ counts.active }} registered · {{ counts.withdrawn }} withdrawn · {{ counts.answered }} answered the questions
            </p>
          </div>
          <GBadge
            v-if="event"
            :tone="event.isPublished ? 'ok' : 'neutral'"
            :icon="event.isPublished ? 'material-symbols:public' : 'material-symbols:draft-rounded'"
            :label="event.isPublished ? 'Published' : 'Draft'"
          />
        </div>

        <GAlert
          v-if="participantsError && !participants.length"
          tone="err"
          title="Couldn't load participants"
          :description="getApiErrorMessage(participantsError, 'Refresh the page to try again.')"
        />

        <div class="wp-toolbar">
          <GTabs
            v-model="filterModel"
            :items="filterTabs"
            :content="false"
            size="sm"
            aria-label="Show participants"
            class="wp-toolbar__filters"
          />
          <GInput
            v-model="searchQuery"
            type="search"
            icon="material-symbols:search-rounded"
            placeholder="Search names, emails, answers"
            aria-label="Search participants"
            class="wp-toolbar__search"
          />
        </div>

        <OrganizersListDetail
          v-model:open="detailOpen"
          :label="`Participant: ${selectedName}`"
        >
          <template #list>
            <div
              v-if="isLoading"
              class="wp-box wp-box--loading"
              aria-busy="true"
            >
              <GSkeleton
                v-for="i in 5"
                :key="i"
                class="wp-skeleton"
              />
            </div>
            <GEmptyState
              v-else-if="!participants.length"
              icon="material-symbols:group-off-rounded"
              title="No one has registered yet"
              :description="event?.isPublished ? 'People who register show up here.' : 'Publish the workshop so people can register.'"
            />
            <GEmptyState
              v-else-if="!visibleParticipants.length"
              icon="material-symbols:search-off-rounded"
              title="No one matches"
              description="Try another search or filter."
            >
              <template #actions>
                <GButton
                  variant="secondary"
                  label="Show everyone"
                  @click="searchQuery = ''; activeFilter = 'all'"
                />
              </template>
            </GEmptyState>
            <ul
              v-else
              class="wp-box wp-list"
              :aria-label="`${visibleParticipants.length} participants`"
            >
              <li
                v-for="(p, index) in visibleParticipants"
                :key="p.userId ?? p.registrationId ?? index"
              >
                <button
                  type="button"
                  class="wp-row"
                  :class="{ 'is-selected': p.userId === selectedUserId }"
                  :aria-current="p.userId === selectedUserId ? 'true' : undefined"
                  @click="select(p.userId)"
                >
                  <span class="wp-row__main">
                    <span class="wp-row__name">{{ displayName(p) }}</span>
                    <span class="wp-row__meta">
                      <template v-if="p.name && p.email">{{ p.email }} · </template>registered {{ waitedFor(new Date(p.registeredAt ?? 0).getTime()) || '—' }}
                    </span>
                  </span>
                  <GBadge
                    v-if="p.withdrawnAt"
                    size="sm"
                    icon="material-symbols:person-off-rounded"
                    label="Withdrawn"
                  />
                </button>
              </li>
            </ul>
          </template>

          <template #detail="{ close }">
            <OrganizersDetailPane
              v-if="selected"
              :title="selectedName"
              close-label="Close participant"
              @close="close"
            >
              <template #meta>
                <GBadge
                  v-if="selected.withdrawnAt"
                  icon="material-symbols:person-off-rounded"
                  label="Withdrawn"
                />
                <GBadge
                  v-else
                  tone="ok"
                  icon="material-symbols:how-to-reg-rounded"
                  :label="selected.status || 'Registered'"
                />
              </template>

              <dl class="wp-facts">
                <div>
                  <dt>Email</dt>
                  <dd>
                    <a
                      v-if="selected.email"
                      :href="`mailto:${selected.email}`"
                      class="wp-link"
                    >{{ selected.email }}</a>
                    <template v-else>
                      —
                    </template>
                  </dd>
                </div>
                <div>
                  <dt>Registered</dt>
                  <dd>{{ formatOrganizerDateTime(selected.registeredAt) }}</dd>
                </div>
                <div v-if="selected.withdrawnAt">
                  <dt>Withdrew</dt>
                  <dd>{{ formatOrganizerDateTime(selected.withdrawnAt) }}</dd>
                </div>
              </dl>

              <section
                class="wp-section"
                aria-labelledby="wp-answers"
              >
                <h3
                  id="wp-answers"
                  class="wp-section__title"
                >
                  Answers
                </h3>
                <OrganizersAnswerList
                  :answers="selectedAnswers"
                  empty-text="No answers. The workshop may not ask any questions."
                />
              </section>

              <section
                class="wp-section"
                aria-labelledby="wp-checkins"
              >
                <h3
                  id="wp-checkins"
                  class="wp-section__title"
                >
                  Check-ins
                </h3>
                <GSkeleton
                  v-if="isLoadingDetail"
                  class="wp-skeleton"
                />
                <ol
                  v-else-if="checkIns.length"
                  class="wp-checkins"
                >
                  <li
                    v-for="(checkIn, index) in checkIns"
                    :key="checkIn.id ?? index"
                  >
                    <span>In {{ formatOrganizerDateTime(checkIn.checkInTime) }}</span>
                    <span class="wp-muted">
                      {{ checkIn.checkOutTime ? `Out ${formatOrganizerDateTime(checkIn.checkOutTime)}` : checkIn.isCheckedIn ? 'Still checked in' : 'No check-out recorded' }}
                    </span>
                  </li>
                </ol>
                <p
                  v-else
                  class="wp-muted"
                >
                  Not checked in yet.
                </p>
              </section>

              <template
                v-if="!selected.withdrawnAt"
                #footer
              >
                <div
                  v-if="!confirmingWithdraw"
                  class="wp-footer"
                >
                  <GButton
                    variant="danger"
                    icon="material-symbols:person-remove-rounded"
                    label="Withdraw from workshop"
                    @click="confirmingWithdraw = true"
                  />
                </div>
                <div
                  v-else
                  class="wp-confirm"
                  role="group"
                  aria-labelledby="wp-confirm-text"
                >
                  <p
                    id="wp-confirm-text"
                    class="wp-confirm__text"
                  >
                    Withdraw <strong>{{ selectedName }}</strong> from this workshop?
                  </p>
                  <GAlert
                    v-if="withdrawError"
                    tone="err"
                    title="Not withdrawn"
                    :description="withdrawError"
                  />
                  <div class="wp-footer">
                    <GButton
                      variant="ghost"
                      label="Keep them"
                      :disabled="withdrawMutation.isPending.value"
                      @click="confirmingWithdraw = false; withdrawError = ''"
                    />
                    <GButton
                      variant="danger"
                      :label="`Withdraw ${selectedName}`"
                      :loading="withdrawMutation.isPending.value"
                      loading-label="Withdrawing"
                      @click="withdraw"
                    />
                  </div>
                </div>
              </template>
            </OrganizersDetailPane>
          </template>

          <template #placeholder>
            <GEmptyState
              icon="material-symbols:person-rounded"
              title="Pick a participant"
              description="Their answers and check-ins show here."
              :bordered="false"
            />
          </template>
        </OrganizersListDetail>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.wp {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 88rem;
  margin: 0 auto;
}
.wp-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.wp-head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}
.wp-head__text {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  min-width: 0;
}
.wp-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 700;
  line-height: 1.2;
  overflow-wrap: anywhere;
}

.wp-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.wp-toolbar__filters { max-width: 100%; overflow-x: auto; }
.wp-toolbar__search { flex: 1 1 18rem; }

.wp-box {
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.wp-box--loading {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
}
.wp-skeleton {
  display: block;
  height: 48px;
}
.wp-list {
  margin: 0;
  padding: 0;
  list-style: none;
  overflow: hidden;
}
@media (min-width: 1024px) {
  .wp-list {
    max-height: calc(100dvh - 8.5rem);
    overflow-y: auto;
  }
}
.wp-list > li { border-top: var(--gh-border-width) solid var(--gh-line-soft); }
.wp-list > li:first-child { border-top: 0; }
.wp-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 68px;
  padding: 10px 16px;
  border: 0;
  border-left: 4px solid transparent;
  background: transparent;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.wp-row:hover { background: var(--gh-ghost-hover); }
.wp-row:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: -2px;
}
.wp-row.is-selected {
  border-left-color: var(--gh-highlight-edge);
  background: var(--gh-highlight);
  color: var(--gh-highlight-ink);
}
.wp-row.is-selected .wp-row__meta { color: inherit; }
.wp-row__main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.wp-row__name { font-weight: 700; overflow-wrap: anywhere; }
.wp-row__meta {
  font-size: 14px;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
@media (prefers-reduced-motion: reduce) {
  .wp-row { transition: none; }
}

.wp-facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 12px 24px;
  margin: 0;
}
.wp-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.wp-facts dd {
  margin: 2px 0 0;
  overflow-wrap: anywhere;
}
.wp-link {
  color: inherit;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.wp-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.wp-section__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.wp-checkins {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.wp-checkins li {
  display: flex;
  flex-wrap: wrap;
  gap: 4px 12px;
  padding-left: 12px;
  border-left: 3px solid var(--gh-line-soft);
  font-size: 14px;
  font-variant-numeric: tabular-nums;
}
.wp-footer {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
}
.wp-confirm {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.wp-confirm__text { margin: 0; }
</style>
