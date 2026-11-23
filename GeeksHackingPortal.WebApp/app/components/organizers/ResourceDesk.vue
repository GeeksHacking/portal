<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListResponseResponseResource,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewParticipantResourceRedemptionDto,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint1QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint2QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint2QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint1QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint2QueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesCreateEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesDeleteEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesRedeemEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesRedeemEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesUpdateEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, parseHackathonDateTimeValue } from '~/utils/hackathon-date-time'

/**
 * The resource desk for one event (meal vouchers, swag, …), shared by hackathons and workshops.
 * Same shape as the check-in desk:
 *
 * - Pick a resource; "Scan QR code" opens the camera. A scan shows the participant, warns if they've
 *   already had this resource, shows the resource's redemption note, then one big Redeem button and
 *   "Scan next".
 * - Counts and the participant list (redeem from the list for people without a code) refresh every
 *   15 seconds and after every redemption; recent activity below.
 * - `manage` (workshops) adds Add / Edit / Delete for the resources themselves.
 */
const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  activityId: string
  /** create, edit and delete resources (the API supports it for workshops) */
  manage?: boolean
}>()

type Resource = GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListResponseResponseResource
type Participant = GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewParticipantResourceRedemptionDto

const toast = useGToast()
const queryClient = useQueryClient()
const isHackathon = computed(() => props.kind === 'hackathon')
const activityId = computed(() => props.activityId)
const LIVE_REFRESH_MS = 15_000

// ---- resources ----------------------------------------------------------------------------------
const listPath = computed(() => ({ activityId: activityId.value }))
const list1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1({ path: listPath }, { query: { enabled: computed(() => isHackathon.value && !!activityId.value) } })
const list2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint2({ path: listPath }, { query: { enabled: computed(() => !isHackathon.value && !!activityId.value) } })
const listQuery = computed(() => isHackathon.value ? list1 : list2)
const resources = computed<Resource[]>(() => listQuery.value.data.value?.resources ?? [])
const isLoadingResources = computed(() => listQuery.value.isLoading.value)

const selectedResourceId = ref('')
watch(resources, (items) => {
  if (selectedResourceId.value && items.some(item => item.id === selectedResourceId.value))
    return
  selectedResourceId.value = items.find(item => item.isPublished)?.id ?? items[0]?.id ?? ''
}, { immediate: true })
const selectedResource = computed(() => resources.value.find(r => r.id === selectedResourceId.value) ?? null)
const canRedeem = computed(() => !!selectedResource.value?.isPublished)

const resourceItems = computed(() => resources.value.map(r => ({
  label: r.isPublished ? (r.name ?? 'Resource') : `${r.name ?? 'Resource'} (unpublished)`,
  value: r.id ?? '',
})))

// ---- overview of the selected resource ----------------------------------------------------------
const overviewPath = computed(() => ({ activityId: activityId.value, resourceId: selectedResourceId.value }))
const overview1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint1({ path: overviewPath }, { query: { enabled: computed(() => isHackathon.value && !!activityId.value && !!selectedResourceId.value), refetchInterval: LIVE_REFRESH_MS } })
const overview2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint2({ path: overviewPath }, { query: { enabled: computed(() => !isHackathon.value && !!activityId.value && !!selectedResourceId.value), refetchInterval: LIVE_REFRESH_MS } })
const overviewQuery = computed(() => isHackathon.value ? overview1 : overview2)
const overview = computed(() => overviewQuery.value.data.value)
const isLoadingOverview = computed(() => !!selectedResourceId.value && overviewQuery.value.isLoading.value)
const isFetchingOverview = computed(() => overviewQuery.value.isFetching.value)
const overviewError = computed(() => overviewQuery.value.error.value)
const updatedAt = computed(() => overviewQuery.value.dataUpdatedAt.value)

const participants = computed<Participant[]>(() => overview.value?.participants ?? [])
const auditTrail = computed(() =>
  [...(overview.value?.auditTrail ?? [])].sort((a, b) =>
    (parseHackathonDateTimeValue(b.timestamp)?.getTime() ?? 0) - (parseHackathonDateTimeValue(a.timestamp)?.getTime() ?? 0)),
)
const counts = computed(() => ({
  total: overview.value?.totalRedemptions ?? 0,
  people: overview.value?.uniqueRedeemers ?? 0,
  notYet: participants.value.filter(p => !p.hasRedeemed).length,
}))

function participantById(userId: string) {
  return participants.value.find(p => p.userId === userId)
}

const searchQuery = ref('')
const redeemedFilter = ref<'all' | 'redeemed' | 'not-yet'>('all')
const redeemedFilters = computed(() => [
  { label: `All ${participants.value.length}`, value: 'all' },
  { label: `Redeemed ${counts.value.people}`, value: 'redeemed' },
  { label: `Not yet ${counts.value.notYet}`, value: 'not-yet' },
])
const redeemedFilterModel = computed({
  get: () => redeemedFilter.value as string,
  set: value => (redeemedFilter.value = value as typeof redeemedFilter.value),
})
const visibleParticipants = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  return participants.value
    .filter(p => redeemedFilter.value === 'all' || (redeemedFilter.value === 'redeemed') === !!p.hasRedeemed)
    .filter(p => !query || (p.userName ?? '').toLowerCase().includes(query))
    // not yet first: they're the ones still to serve
    .sort((a, b) => Number(!!a.hasRedeemed) - Number(!!b.hasRedeemed)
      || (a.userName ?? '').localeCompare(b.userName ?? '', undefined, { sensitivity: 'base' }))
})

const timeFormatter = new Intl.DateTimeFormat(undefined, { weekday: 'short', hour: 'numeric', minute: '2-digit', timeZone: HACKATHON_TIME_ZONE })
function formatTime(value: Date | string | null | undefined) {
  const date = parseHackathonDateTimeValue(value)
  return date ? `${timeFormatter.format(date)} ${HACKATHON_TIME_ZONE_LABEL}` : ''
}
function times(n: number | undefined) {
  return n === 1 ? 'once' : n === 2 ? 'twice' : `${n ?? 0} times`
}
function redeemedSummary(p: Participant) {
  return p.hasRedeemed ? `Redeemed ${times(p.redemptionCount)} · last ${formatTime(p.lastRedeemedAt)}` : 'Not redeemed yet'
}
const updatedLabel = computed(() => updatedAt.value
  ? new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', second: '2-digit' }).format(new Date(updatedAt.value))
  : '')

// ---- refresh + redeem ---------------------------------------------------------------------------
const historyUserId = ref('')

function refresh() {
  const path = { path: { activityId: activityId.value, resourceId: selectedResourceId.value } }
  if (selectedResourceId.value) {
    queryClient.invalidateQueries({
      queryKey: isHackathon.value
        ? geeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint1QueryKey(path)
        : geeksHackingPortalApiEndpointsOrganizersHackathonResourcesOverviewEndpoint2QueryKey(path),
    })
  }
  if (selectedResourceId.value && historyUserId.value) {
    const historyPath = { path: { ...path.path, participantUserId: historyUserId.value } }
    queryClient.invalidateQueries({
      queryKey: isHackathon.value
        ? geeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint1QueryKey(historyPath)
        : geeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint2QueryKey(historyPath),
    })
  }
}

function refreshResources() {
  const path = { path: { activityId: activityId.value } }
  queryClient.invalidateQueries({
    queryKey: isHackathon.value
      ? geeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1QueryKey(path)
      : geeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint2QueryKey(path),
  })
  refresh()
}

const redeem1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesRedeemEndpoint1()
const redeem2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesRedeemEndpoint2()

async function redeem(userId: string) {
  const path = { activityId: activityId.value, participantUserId: userId, resourceId: selectedResourceId.value }
  const result = isHackathon.value ? await redeem1.mutateAsync({ path }) : await redeem2.mutateAsync({ path })
  refresh()
  return result
}

const pendingUserId = ref<string | null>(null)
async function redeemFromList(p: Participant) {
  if (!p.userId || pendingUserId.value || !canRedeem.value)
    return
  pendingUserId.value = p.userId
  try {
    await redeem(p.userId)
    toast.add({ title: `${selectedResource.value?.name ?? 'Resource'} redeemed`, description: `For ${p.userName ?? 'participant'}.`, tone: 'ok' })
  }
  catch (error) {
    console.error('Redeem from list failed', error)
    toast.add({ title: 'Not redeemed', description: getApiErrorMessage(error, 'Try again in a moment.'), tone: 'err' })
  }
  finally {
    pendingUserId.value = null
  }
}

const auditLimit = ref(15)
watch(selectedResourceId, () => {
  auditLimit.value = 15
  searchQuery.value = ''
  redeemedFilter.value = 'all'
})

// ---- history dialog -----------------------------------------------------------------------------
const isHistoryOpen = ref(false)
const historyName = ref('')
const historyPath = computed(() => ({ activityId: activityId.value, participantUserId: historyUserId.value, resourceId: selectedResourceId.value }))
const history1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint1({ path: historyPath }, { query: { enabled: computed(() => isHackathon.value && !!historyUserId.value && !!selectedResourceId.value) } })
const history2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesHistoryEndpoint2({ path: historyPath }, { query: { enabled: computed(() => !isHackathon.value && !!historyUserId.value && !!selectedResourceId.value) } })
const historyQuery = computed(() => isHackathon.value ? history1 : history2)
const historyItems = computed(() =>
  [...(historyQuery.value.data.value?.history ?? [])].sort((a, b) =>
    (parseHackathonDateTimeValue(b.createdAt)?.getTime() ?? 0) - (parseHackathonDateTimeValue(a.createdAt)?.getTime() ?? 0)),
)

function openHistory(userId: string, name: string) {
  historyUserId.value = userId
  historyName.value = name
  isHistoryOpen.value = true
}

// ---- scanner ------------------------------------------------------------------------------------
const isScannerOpen = ref(false)
const scannedUserId = ref('')
const scanError = ref('')
const rawQrData = ref('')
const scanAction = ref<{ ok: boolean, title: string, message: string } | null>(null)
const isActing = ref(false)

const detail1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpoint({ path: computed(() => ({ hackathonId: activityId.value, userId: scannedUserId.value })) }, { query: { enabled: computed(() => isHackathon.value && !!scannedUserId.value && !participantById(scannedUserId.value)), retry: false } })
const detail2 = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint({ path: computed(() => ({ standaloneWorkshopId: activityId.value, userId: scannedUserId.value })) }, { query: { enabled: computed(() => !isHackathon.value && !!scannedUserId.value && !participantById(scannedUserId.value)), retry: false } })

const scanned = computed(() => participantById(scannedUserId.value))
const scannedName = computed(() => scanned.value?.userName || (isHackathon.value ? detail1.data.value?.name : detail2.data.value?.name) || 'Participant')
const scannedIsUnknown = computed(() => !!scannedUserId.value && !isLoadingOverview.value && !scanned.value)

function onScan(text: string) {
  rawQrData.value = text
  try {
    scannedUserId.value = readParticipantQr(text, activityId.value, participants.value)
  }
  catch (error) {
    scanError.value = (error as Error).message
  }
}

function scanNext() {
  scannedUserId.value = ''
  scanAction.value = null
  scanError.value = ''
  rawQrData.value = ''
}

async function redeemScanned() {
  const userId = scannedUserId.value
  if (!userId || isActing.value)
    return
  isActing.value = true
  scanAction.value = null
  try {
    const result = await redeem(userId)
    const at = formatTime((result as { createdAt?: string | null })?.createdAt)
    scanAction.value = { ok: true, title: `${selectedResource.value?.name ?? 'Resource'} redeemed`, message: `For ${scannedName.value}${at ? ` at ${at}` : ''}.` }
  }
  catch (error) {
    console.error('Redeem from scan failed', error)
    scanAction.value = { ok: false, title: 'Not redeemed', message: getApiErrorMessage(error, 'Check the code and try again.') }
  }
  finally {
    isActing.value = false
  }
}

watch(isScannerOpen, (open) => {
  if (!open)
    scanNext()
})

// ---- manage resources (workshops) ---------------------------------------------------------------
const createMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesCreateEndpoint2()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesUpdateEndpoint2()
const deleteMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesDeleteEndpoint2()

const isFormOpen = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({ name: '', description: '', redemptionStmt: '', isPublished: true })
const formError = ref('')
const showFormErrors = ref(false)
const nameError = computed(() => showFormErrors.value && !form.name.trim() ? 'Give the resource a name.' : '')
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

function openCreate() {
  editingId.value = null
  Object.assign(form, { name: '', description: '', redemptionStmt: '', isPublished: true })
  formError.value = ''
  showFormErrors.value = false
  isFormOpen.value = true
}

function openEdit(resource: Resource) {
  editingId.value = resource.id ?? null
  Object.assign(form, {
    name: resource.name ?? '',
    description: resource.description ?? '',
    redemptionStmt: resource.redemptionStmt ?? '',
    isPublished: resource.isPublished ?? false,
  })
  formError.value = ''
  showFormErrors.value = false
  isFormOpen.value = true
}

async function saveResource() {
  formError.value = ''
  if (!form.name.trim()) {
    showFormErrors.value = true
    return
  }
  const body = {
    name: form.name.trim(),
    description: form.description.trim() || null,
    redemptionStmt: form.redemptionStmt.trim() || undefined,
    isPublished: form.isPublished,
  }
  try {
    if (editingId.value) {
      await updateMutation.mutateAsync({ path: { activityId: activityId.value, resourceId: editingId.value }, body: { ...body, redemptionStmt: body.redemptionStmt ?? null } })
      toast.add({ title: 'Resource saved', tone: 'ok' })
    }
    else {
      const created = await createMutation.mutateAsync({ path: { activityId: activityId.value }, body })
      toast.add({ title: 'Resource added', tone: 'ok' })
      const createdId = (created as { id?: string } | undefined)?.id
      if (createdId)
        selectedResourceId.value = createdId
    }
    isFormOpen.value = false
    refreshResources()
  }
  catch (error) {
    console.error('Failed to save resource', error)
    formError.value = getApiErrorMessage(error, 'The resource wasn\'t saved. Try again.')
  }
}

const isDeleteOpen = ref(false)
const deleteError = ref('')
function openDelete() {
  deleteError.value = ''
  isDeleteOpen.value = true
}
async function deleteResource() {
  const resource = selectedResource.value
  if (!resource?.id)
    return
  deleteError.value = ''
  try {
    await deleteMutation.mutateAsync({ path: { activityId: activityId.value, resourceId: resource.id } })
    isDeleteOpen.value = false
    selectedResourceId.value = ''
    toast.add({ title: `${resource.name ?? 'Resource'} deleted`, tone: 'ok' })
    refreshResources()
  }
  catch (error) {
    console.error('Failed to delete resource', error)
    deleteError.value = getApiErrorMessage(error, 'The resource wasn\'t deleted. Try again.')
  }
}
</script>

<template>
  <div class="rd">
    <GLoading
      v-if="isLoadingResources"
      label="Loading resources…"
    />

    <!-- no resources yet -->
    <GEmptyState
      v-else-if="!resources.length"
      icon="material-symbols:redeem-rounded"
      title="No resources yet"
      :description="manage ? 'Add things to hand out, like meal vouchers or swag, then redeem them by scanning participants\' QR codes.' : 'This hackathon has no resources to hand out yet.'"
      bordered
    >
      <template
        v-if="manage"
        #actions
      >
        <GButton
          variant="primary"
          icon="material-symbols:add-rounded"
          label="Add resource"
          @click="openCreate"
        />
      </template>
    </GEmptyState>

    <template v-else>
      <!-- pick + scan -->
      <div class="rd-top">
        <GFormField
          label="Resource"
          class="rd-top__pick"
        >
          <GSelect
            v-model="selectedResourceId"
            :items="resourceItems"
            icon="material-symbols:redeem-rounded"
          />
        </GFormField>
        <GButton
          variant="primary"
          size="lg"
          icon="material-symbols:qr-code-scanner-rounded"
          label="Scan QR code"
          class="rd-top__scan"
          :disabled="!canRedeem"
          @click="isScannerOpen = true"
        />
        <GButton
          v-if="manage"
          variant="secondary"
          size="lg"
          icon="material-symbols:add-rounded"
          label="Add resource"
          class="rd-top__add"
          @click="openCreate"
        />
      </div>

      <!-- the selected resource -->
      <section
        v-if="selectedResource"
        class="rd-card"
        aria-labelledby="rd-resource-title"
      >
        <div class="rd-card__head">
          <div class="flex min-w-0 flex-col gap-2">
            <div class="flex flex-wrap items-center gap-2">
              <h2
                id="rd-resource-title"
                class="rd-card__title"
              >
                {{ selectedResource.name }}
              </h2>
              <GBadge
                :tone="selectedResource.isPublished ? 'ok' : 'neutral'"
                :variant="selectedResource.isPublished ? 'soft' : 'outline'"
                :label="selectedResource.isPublished ? 'Published' : 'Unpublished'"
              />
            </div>
            <p
              v-if="selectedResource.description"
              class="rd-muted"
            >
              {{ selectedResource.description }}
            </p>
          </div>
          <div
            v-if="manage"
            class="rd-card__tools"
          >
            <GButton
              variant="ghost"
              size="sm"
              icon="material-symbols:edit-rounded"
              label="Edit"
              @click="openEdit(selectedResource)"
            />
            <GButton
              variant="ghost"
              size="sm"
              icon="material-symbols:delete-rounded"
              label="Delete"
              @click="openDelete"
            />
          </div>
        </div>
        <p
          v-if="selectedResource.redemptionStmt"
          class="rd-note"
        >
          <span class="rd-note__label">When redeeming</span>
          {{ selectedResource.redemptionStmt }}
        </p>
        <GAlert
          v-if="!selectedResource.isPublished"
          tone="attention"
          title="Unpublished"
          :description="manage ? 'Publish it to start redeeming. You can still look at past redemptions.' : 'It can\'t be redeemed until it\'s published. You can still look at past redemptions.'"
        />
      </section>

      <div class="rd-live">
        <p
          class="rd-live__updated"
          aria-live="polite"
        >
          <span
            class="rd-live__dot"
            :class="{ 'rd-live__dot--busy': isFetchingOverview }"
            aria-hidden="true"
          />
          {{ updatedLabel ? `Live · updated ${updatedLabel}` : 'Loading…' }}
        </p>
        <GButton
          variant="ghost"
          size="sm"
          icon="material-symbols:refresh-rounded"
          label="Refresh"
          :loading="isFetchingOverview"
          loading-label="Refreshing"
          @click="refresh"
        />
      </div>

      <GAlert
        v-if="overviewError && !participants.length"
        tone="err"
        title="Couldn't load redemptions"
        :description="getApiErrorMessage(overviewError, 'Refresh to try again.')"
      />

      <div class="rd-stats">
        <GStatTile
          label="People served"
          :value="counts.people"
          :hint="participants.length ? `of ${participants.length} participants` : undefined"
          icon="material-symbols:how-to-reg-rounded"
          highlight
          :loading="isLoadingOverview"
        />
        <GStatTile
          label="Not yet"
          :value="counts.notYet"
          icon="material-symbols:schedule-rounded"
          :loading="isLoadingOverview"
        />
        <GStatTile
          label="Redemptions"
          :value="counts.total"
          icon="material-symbols:redeem-rounded"
          :loading="isLoadingOverview"
        />
      </div>

      <!-- participants -->
      <section
        class="rd-section"
        aria-labelledby="rd-people-title"
      >
        <h2
          id="rd-people-title"
          class="rd-section__title"
        >
          Participants
        </h2>
        <GInput
          v-model="searchQuery"
          type="search"
          icon="material-symbols:search-rounded"
          placeholder="Search by name"
          aria-label="Search participants by name"
        />
        <GTabs
          v-model="redeemedFilterModel"
          :items="redeemedFilters"
          :content="false"
          size="sm"
          aria-label="Show participants"
          class="self-start"
        />

        <div
          v-if="isLoadingOverview"
          class="rd-list"
          aria-busy="true"
        >
          <GSkeleton
            v-for="i in 5"
            :key="i"
            class="rd-skeleton"
          />
        </div>
        <GEmptyState
          v-else-if="!participants.length"
          icon="material-symbols:group-off-rounded"
          title="No participants yet"
          :description="kind === 'hackathon' ? 'Accepted participants show up here.' : 'People who register show up here.'"
          bordered
        />
        <GEmptyState
          v-else-if="!visibleParticipants.length"
          icon="material-symbols:search-off-rounded"
          title="No one matches"
          description="Try another name, or show everyone."
          bordered
        >
          <template #actions>
            <GButton
              variant="secondary"
              label="Show everyone"
              @click="searchQuery = ''; redeemedFilter = 'all'"
            />
          </template>
        </GEmptyState>
        <ul
          v-else
          class="rd-list"
        >
          <li
            v-for="(p, index) in visibleParticipants"
            :key="p.userId ?? p.participantId ?? index"
            class="rd-row"
          >
            <div class="rd-row__who">
              <p class="rd-row__name">
                {{ p.userName || 'Unnamed participant' }}
              </p>
              <div class="rd-row__meta">
                <GBadge
                  :tone="p.hasRedeemed ? 'ok' : 'neutral'"
                  :icon="p.hasRedeemed ? 'material-symbols:check-circle-rounded' : undefined"
                  :label="p.hasRedeemed ? (p.redemptionCount && p.redemptionCount > 1 ? `Redeemed ×${p.redemptionCount}` : 'Redeemed') : 'Not yet'"
                  size="sm"
                />
                <span v-if="p.hasRedeemed">Last {{ formatTime(p.lastRedeemedAt) }}</span>
              </div>
            </div>
            <div class="rd-row__actions">
              <GButton
                :variant="p.hasRedeemed ? 'secondary' : 'primary'"
                size="sm"
                icon="material-symbols:redeem-rounded"
                :label="p.hasRedeemed ? 'Redeem again' : 'Redeem'"
                :aria-label="`${p.hasRedeemed ? 'Redeem again' : 'Redeem'} ${selectedResource?.name ?? 'resource'} for ${p.userName ?? 'participant'}`"
                :loading="pendingUserId === p.userId"
                loading-label="Redeeming"
                :disabled="!canRedeem || !p.userId || (!!pendingUserId && pendingUserId !== p.userId)"
                @click="redeemFromList(p)"
              />
              <GButton
                variant="ghost"
                size="sm"
                icon="material-symbols:history-rounded"
                :aria-label="`Redemption history for ${p.userName ?? 'participant'}`"
                title="History"
                :disabled="!p.userId"
                @click="openHistory(p.userId ?? '', p.userName ?? 'Participant')"
              />
            </div>
          </li>
        </ul>
      </section>

      <!-- activity -->
      <section
        class="rd-section"
        aria-labelledby="rd-activity-title"
      >
        <h2
          id="rd-activity-title"
          class="rd-section__title"
        >
          Recent activity
        </h2>
        <p
          v-if="!auditTrail.length"
          class="rd-muted"
        >
          Redemptions show up here as they happen.
        </p>
        <template v-else>
          <ol class="rd-feed">
            <li
              v-for="(event, index) in auditTrail.slice(0, auditLimit)"
              :key="event.redemptionId ?? index"
            >
              <span class="rd-feed__time">{{ formatTime(event.timestamp) }}</span>
              <span><strong>{{ event.userName ?? 'Participant' }}</strong> redeemed {{ selectedResource?.name ?? 'it' }}</span>
            </li>
          </ol>
          <GButton
            v-if="auditTrail.length > auditLimit"
            variant="ghost"
            size="sm"
            :label="`Show all ${auditTrail.length}`"
            class="self-start"
            @click="auditLimit = auditTrail.length"
          />
        </template>
      </section>
    </template>
  </div>

  <!-- scanner -->
  <GModal
    v-model:open="isScannerOpen"
    :title="selectedResource?.name ? `Redeem ${selectedResource.name}` : 'Redeem'"
    :description="scannedUserId || scanError ? undefined : 'Point the camera at the participant\'s QR code.'"
    size="md"
  >
    <div class="rd-scan">
      <OrganizersQrScanner
        v-if="!scannedUserId && !scanError"
        @decoded="onScan"
      />

      <template v-if="scanError">
        <GAlert
          tone="err"
          title="Can't use this code"
          :description="scanError"
        />
        <details
          v-if="rawQrData"
          class="rd-scan__raw"
        >
          <summary>Show what the code contains</summary>
          <pre>{{ rawQrData }}</pre>
        </details>
        <GButton
          variant="primary"
          size="lg"
          block
          icon="material-symbols:qr-code-scanner-rounded"
          label="Scan again"
          @click="scanNext"
        />
      </template>

      <template v-else-if="scannedUserId">
        <div class="rd-scan__person">
          <p class="rd-scan__name">
            {{ scannedName }}
          </p>
          <p
            v-if="scanned"
            class="rd-row__meta"
          >
            {{ redeemedSummary(scanned) }}
          </p>
        </div>

        <GAlert
          v-if="scannedIsUnknown && !scanAction"
          tone="attention"
          title="Not on the participant list"
          :description="kind === 'hackathon' ? 'Only accepted participants can redeem resources.' : 'They may not be registered for this workshop.'"
        />
        <GAlert
          v-else-if="scanned?.hasRedeemed && !scanAction"
          tone="attention"
          :title="`Already redeemed ${times(scanned.redemptionCount)}`"
          :description="`Last at ${formatTime(scanned.lastRedeemedAt)}. Only redeem again if they're allowed more than one.`"
        />
        <p
          v-if="selectedResource?.redemptionStmt && !scanAction?.ok"
          class="rd-note"
        >
          <span class="rd-note__label">When redeeming</span>
          {{ selectedResource.redemptionStmt }}
        </p>

        <GAlert
          v-if="scanAction"
          :tone="scanAction.ok ? 'ok' : 'err'"
          :title="scanAction.title"
          :description="scanAction.message || undefined"
        />

        <div class="rd-scan__actions">
          <GButton
            v-if="!scanAction?.ok"
            :variant="scanned?.hasRedeemed ? 'secondary' : 'primary'"
            size="lg"
            block
            icon="material-symbols:redeem-rounded"
            :label="scanned?.hasRedeemed ? 'Redeem again' : 'Redeem'"
            :loading="isActing"
            loading-label="Redeeming"
            :disabled="!canRedeem"
            @click="redeemScanned"
          />
          <GButton
            :variant="scanAction?.ok ? 'primary' : 'ghost'"
            :size="scanAction?.ok ? 'lg' : 'md'"
            block
            icon="material-symbols:qr-code-scanner-rounded"
            label="Scan next"
            @click="scanNext"
          />
          <GButton
            variant="ghost"
            block
            icon="material-symbols:history-rounded"
            label="History"
            @click="openHistory(scannedUserId, scannedName)"
          />
        </div>
      </template>
    </div>
  </GModal>

  <!-- one participant's history for this resource -->
  <GModal
    v-model:open="isHistoryOpen"
    :title="historyName || 'Participant'"
    :description="selectedResource?.name ? `${selectedResource.name} redemptions, newest first.` : 'Redemptions, newest first.'"
    size="sm"
  >
    <GLoading
      v-if="historyQuery.isLoading.value"
      label="Loading history…"
    />
    <GAlert
      v-else-if="historyQuery.error.value"
      tone="err"
      title="Couldn't load the history"
      :description="getApiErrorMessage(historyQuery.error.value, 'Try again in a moment.')"
    />
    <ol
      v-else-if="historyItems.length"
      class="rd-feed"
    >
      <li
        v-for="(item, index) in historyItems"
        :key="item.redemptionId ?? index"
      >
        <span class="rd-feed__time">{{ formatTime(item.createdAt) }}</span>
        <span>Redeemed</span>
      </li>
    </ol>
    <p
      v-else
      class="rd-muted"
    >
      Not redeemed yet.
    </p>
  </GModal>

  <!-- add / edit (workshops) -->
  <GModal
    v-if="manage"
    v-model:open="isFormOpen"
    :title="editingId ? 'Edit resource' : 'Add resource'"
    description="Something you hand out to participants, like a meal voucher or swag."
    size="md"
  >
    <form
      id="rd-resource-form"
      class="flex flex-col gap-4"
      novalidate
      @submit.prevent="saveResource"
    >
      <GFormField
        label="Name"
        required
        :error="nameError"
      >
        <GInput
          v-model="form.name"
          placeholder="Lunch voucher"
          :invalid="!!nameError"
          required
        />
      </GFormField>
      <GFormField
        label="Description"
        hint="Optional. Shown to organisers on this page."
      >
        <GTextarea
          v-model="form.description"
          :rows="2"
          autoresize
        />
      </GFormField>
      <GFormField
        label="Note when redeeming"
        hint="Optional. Shown to whoever is redeeming, e.g. “Check their dietary preference”."
      >
        <GTextarea
          v-model="form.redemptionStmt"
          :rows="2"
          autoresize
        />
      </GFormField>
      <GSwitch
        v-model="form.isPublished"
        label="Published"
        description="Only published resources can be redeemed."
      />
      <GAlert
        v-if="formError"
        tone="err"
        title="Not saved"
        :description="formError"
      />
    </form>
    <template #footer>
      <GButton
        variant="ghost"
        label="Cancel"
        @click="isFormOpen = false"
      />
      <GButton
        type="submit"
        form="rd-resource-form"
        variant="primary"
        :label="editingId ? 'Save' : 'Add resource'"
        :loading="isSaving"
        loading-label="Saving"
      />
    </template>
  </GModal>

  <GModal
    v-if="manage"
    v-model:open="isDeleteOpen"
    :title="`Delete ${selectedResource?.name ?? 'this resource'}?`"
    description="Its redemption history is deleted too. This can't be undone."
    size="sm"
  >
    <GAlert
      v-if="deleteError"
      tone="err"
      title="Not deleted"
      :description="deleteError"
    />
    <template #footer>
      <GButton
        variant="ghost"
        label="Keep it"
        @click="isDeleteOpen = false"
      />
      <GButton
        variant="danger"
        label="Delete"
        :loading="deleteMutation.isPending.value"
        loading-label="Deleting"
        @click="deleteResource"
      />
    </template>
  </GModal>
</template>

<style scoped>
.rd {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.rd-muted {
  margin: 0;
  color: var(--gh-text-muted);
}

.rd-top {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 12px;
}
.rd-top__pick { flex: 1 1 18rem; min-width: 0; }
.rd-top__scan { flex: 1 1 14rem; }
@media (min-width: 640px) {
  .rd-top__scan, .rd-top__add { flex: 0 0 auto; }
}

.rd-card {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.rd-card__head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px 16px;
}
.rd-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.rd-card__tools {
  display: flex;
  gap: 4px;
  margin: -6px -8px 0 0;
}
/* the redemption note: read it before handing anything over */
.rd-note {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin: 0;
  padding: 10px 14px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-left: 6px solid var(--gh-accent);
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface-sunken);
  white-space: pre-line;
}
.rd-note__label {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}

.rd-live {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  margin-bottom: -12px;
}
.rd-live__updated {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}
.rd-live__dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: var(--gh-ok);
}
.rd-live__dot--busy { animation: rd-pulse 1s ease-in-out infinite; }
@keyframes rd-pulse { 50% { opacity: 0.3; } }
@media (prefers-reduced-motion: reduce) {
  .rd-live__dot--busy { animation: none; }
}

.rd-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}
@media (max-width: 479px) {
  .rd-stats { grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); }
  .rd-stats > :first-child { grid-column: 1 / -1; }
}

.rd-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.rd-section__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
}
.rd-section :deep(.g-tabs) { max-width: 100%; overflow-x: auto; }

.rd-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.rd-list[aria-busy] {
  gap: 8px;
  padding: 12px;
}
.rd-skeleton {
  display: block;
  height: 48px;
}
.rd-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.rd-row:first-child { border-top: 0; }
.rd-row__who {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
  flex: 1 1 14rem;
}
.rd-row__name {
  margin: 0;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.rd-row__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.rd-row__actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.rd-feed {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.rd-feed li {
  display: grid;
  grid-template-columns: 9.5rem minmax(0, 1fr);
  gap: 12px;
  font-size: 14px;
}
.rd-feed__time {
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}
@media (max-width: 479px) {
  .rd-feed li { grid-template-columns: minmax(0, 1fr); gap: 0; }
}

.rd-scan {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.rd-scan__person {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.rd-scan__name {
  margin: 0;
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 700;
  line-height: 1.15;
  overflow-wrap: anywhere;
}
.rd-scan__actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.rd-scan__raw { font-size: 14px; }
.rd-scan__raw summary { cursor: pointer; }
.rd-scan__raw pre {
  margin: 8px 0 0;
  padding: 8px;
  overflow: auto;
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface-sunken);
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
