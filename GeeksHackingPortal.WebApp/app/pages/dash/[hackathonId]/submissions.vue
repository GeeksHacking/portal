<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListSubmissionItem } from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsGetEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueries } from '@tanstack/vue-query'
import * as XLSX from 'xlsx'
import { formatOrganizerDateTime } from '~/utils/participant-review'

/**
 * Hackathon submissions: filter by challenge, search (titles, teams, descriptions), export to
 * Excel; pick one to read it and open its links in the side panel. Details (description and
 * links) are fetched for every submission, as before, so search and export can use them.
 */
type SubmissionItem = GeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListSubmissionItem
interface Submission extends SubmissionItem {
  description?: string
  repositoryUri?: string
  demoUri?: string
  slidesUri?: string
  detailsLoaded: boolean
}

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')
const path = computed(() => ({ hackathonId: hackathonId.value }))

const { data: submissionsData, isLoading, error: loadError } = useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListEndpoint({ path })
const { data: challengesData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint({ path })

const list = computed<SubmissionItem[]>(() => submissionsData.value?.submissions ?? [])

const detailQueries = useQueries({
  queries: computed(() => list.value.map(s =>
    geeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsGetEndpointQueryOptions({ path: { hackathonId: hackathonId.value, submissionId: s.id ?? '' } }))),
})

const submissions = computed<Submission[]>(() => {
  const details = new Map(detailQueries.value.filter(q => q.data?.id).map(q => [q.data!.id as string, q.data!]))
  return list.value.map((s) => {
    const d = details.get(s.id ?? '')
    return {
      ...s,
      description: d?.description,
      repositoryUri: d?.repositoryUri,
      demoUri: d?.demoUri,
      slidesUri: d?.slidesUri,
      detailsLoaded: !!d,
    }
  })
})
const detailsLoading = computed(() => detailQueries.value.some(q => q.isLoading))

// The API stores a placeholder when no demo link was given: don't show it as a real link
function realLink(uri: string | null | undefined) {
  if (!uri)
    return ''
  try {
    return new URL(uri).hostname === 'example.com' ? '' : uri
  }
  catch {
    return uri
  }
}

function linksOf(s: Submission) {
  return [
    { label: 'Repository', icon: 'material-symbols:code-rounded', href: realLink(s.repositoryUri) },
    { label: 'Slides', icon: 'material-symbols:slideshow-rounded', href: realLink(s.slidesUri) },
    { label: 'Demo', icon: 'material-symbols:play-circle-rounded', href: realLink(s.demoUri) },
  ].filter(link => link.href)
}

// ---- filters ----
const searchQuery = ref('')
const challengeFilter = ref<string>('all')
const challengeOptions = computed(() => [
  { label: 'All challenges', value: 'all' },
  ...(challengesData.value?.challenges ?? []).filter(c => c.id).map(c => ({ label: c.title || 'Untitled challenge', value: c.id as string })),
])

const visibleSubmissions = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  return submissions.value
    .filter(s => challengeFilter.value === 'all' || s.challengeId === challengeFilter.value)
    .filter(s => !query || [s.title, s.teamName, s.challengeTitle, s.description].join(' ').toLowerCase().includes(query))
    .sort((a, b) => new Date(b.submittedAt ?? 0).getTime() - new Date(a.submittedAt ?? 0).getTime())
})

function clearFilters() {
  searchQuery.value = ''
  challengeFilter.value = 'all'
}

// ---- selection ----
const selectedId = ref<string | null>(null)
const detailOpen = computed({
  get: () => !!selectedId.value,
  set: (value) => {
    if (!value)
      selectedId.value = null
  },
})
const selected = computed(() => submissions.value.find(s => s.id === selectedId.value) ?? null)

// ?submission=<id> (e.g. from a team's panel) opens it once the list loads
watch([list, () => route.query.submission], ([items, wanted]) => {
  if (typeof wanted === 'string' && wanted && !selectedId.value && items.some(s => s.id === wanted)) {
    clearFilters()
    selectedId.value = wanted
  }
}, { immediate: true })

// ---- export ----
function exportToExcel() {
  const rows = visibleSubmissions.value.map(s => ({
    'Title': s.title ?? '',
    'Team': s.teamName ?? '',
    'Challenge': s.challengeTitle ?? 'No challenge',
    'Description': s.description ?? '',
    'Repository URL': s.repositoryUri ?? '',
    'Demo URL': s.demoUri ?? '',
    'Slides URL': s.slidesUri ?? '',
    'Submitted At': s.submittedAt ? new Date(s.submittedAt).toISOString() : '',
  }))
  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(workbook, XLSX.utils.json_to_sheet(rows), 'Submissions')
  XLSX.writeFile(workbook, `submissions-${new Date().toISOString().split('T')[0]}.xlsx`)
}
</script>

<template>
  <GPanel id="submissions">
    <template #header>
      <GPanelHeader title="Submissions">
        <template #right>
          <GButton
            variant="secondary"
            icon="material-symbols:download-rounded"
            :label="visibleSubmissions.length === submissions.length ? 'Export to Excel' : `Export ${visibleSubmissions.length} to Excel`"
            :disabled="!visibleSubmissions.length"
            :loading="detailsLoading"
            loading-label="Loading details"
            @click="exportToExcel"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="sb">
        <GAlert
          v-if="loadError && !list.length"
          tone="err"
          title="Couldn't load submissions"
          :description="getApiErrorMessage(loadError, 'Refresh the page to try again.')"
        />

        <div class="sb-toolbar">
          <GSelect
            v-model="challengeFilter"
            :items="challengeOptions"
            aria-label="Challenge"
            class="sb-toolbar__select"
          />
          <GInput
            v-model="searchQuery"
            type="search"
            icon="material-symbols:search-rounded"
            placeholder="Search titles, teams, descriptions"
            aria-label="Search submissions"
            class="sb-toolbar__search"
          />
        </div>
        <p
          v-if="submissions.length"
          class="sb-muted"
          aria-live="polite"
        >
          {{ visibleSubmissions.length === submissions.length
            ? `${submissions.length} ${submissions.length === 1 ? 'submission' : 'submissions'}`
            : `${visibleSubmissions.length} of ${submissions.length} submissions` }}
        </p>

        <OrganizersListDetail
          v-model:open="detailOpen"
          :label="`Submission: ${selected?.title || 'project'}`"
        >
          <template #list>
            <div
              v-if="isLoading"
              class="sb-box sb-box--loading"
              aria-busy="true"
            >
              <GSkeleton
                v-for="i in 5"
                :key="i"
                class="sb-skeleton"
              />
            </div>
            <GEmptyState
              v-else-if="!list.length && !loadError"
              icon="material-symbols:description-rounded"
              title="No submissions yet"
              description="Teams submit from their event page once submissions open."
            />
            <GEmptyState
              v-else-if="!visibleSubmissions.length && list.length"
              icon="material-symbols:search-off-rounded"
              title="No submissions match"
              description="Try another search or challenge."
            >
              <template #actions>
                <GButton
                  variant="secondary"
                  label="Show all submissions"
                  @click="clearFilters"
                />
              </template>
            </GEmptyState>
            <ul
              v-else-if="visibleSubmissions.length"
              class="sb-box sb-list"
            >
              <li
                v-for="s in visibleSubmissions"
                :key="s.id"
              >
                <button
                  type="button"
                  class="sb-row"
                  :class="{ 'is-selected': s.id === selectedId }"
                  :aria-current="s.id === selectedId ? 'true' : undefined"
                  @click="selectedId = s.id ?? null"
                >
                  <span class="sb-row__title">{{ s.title || 'Untitled project' }}</span>
                  <span class="sb-row__meta">{{ s.teamName || 'Unknown team' }} · {{ s.challengeTitle || 'No challenge' }}</span>
                  <span class="sb-row__meta">{{ formatOrganizerDateTime(s.submittedAt) }}</span>
                </button>
              </li>
            </ul>
          </template>

          <template #detail="{ close }">
            <OrganizersDetailPane
              v-if="selected"
              :title="selected.title || 'Untitled project'"
              close-label="Close submission"
              @close="close"
            >
              <template #meta>
                <GBadge
                  :label="selected.challengeTitle || 'No challenge'"
                  icon="material-symbols:trophy-rounded"
                />
              </template>

              <dl class="sb-facts">
                <div>
                  <dt>Team</dt>
                  <dd>
                    <NuxtLink
                      v-if="selected.teamId"
                      :to="{ path: `/dash/${hackathonId}/teams`, query: { team: selected.teamId } }"
                      class="sb-link"
                    >
                      {{ selected.teamName || 'Unknown team' }}
                    </NuxtLink>
                    <template v-else>
                      {{ selected.teamName || 'Unknown team' }}
                    </template>
                  </dd>
                </div>
                <div>
                  <dt>Submitted</dt>
                  <dd>{{ formatOrganizerDateTime(selected.submittedAt) }}</dd>
                </div>
              </dl>

              <GSkeleton
                v-if="!selected.detailsLoaded"
                class="sb-skeleton"
              />
              <template v-else>
                <section
                  v-if="linksOf(selected).length"
                  class="sb-section"
                  aria-labelledby="sb-links"
                >
                  <h3
                    id="sb-links"
                    class="sb-section__title"
                  >
                    Links
                  </h3>
                  <ul class="sb-links">
                    <li
                      v-for="link in linksOf(selected)"
                      :key="link.label"
                    >
                      <a
                        :href="link.href"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="sb-tile"
                      >
                        <Icon
                          :name="link.icon"
                          class="sb-tile__icon"
                          aria-hidden="true"
                        />
                        <span class="sb-tile__text">
                          <span class="sb-tile__label">{{ link.label }}</span>
                          <span class="sb-tile__url">{{ link.href }}</span>
                        </span>
                        <Icon
                          name="material-symbols:open-in-new-rounded"
                          class="sb-tile__open"
                          aria-hidden="true"
                        />
                        <span class="sr-only">(opens in a new tab)</span>
                      </a>
                    </li>
                  </ul>
                </section>

                <section
                  class="sb-section"
                  aria-labelledby="sb-summary"
                >
                  <h3
                    id="sb-summary"
                    class="sb-section__title"
                  >
                    Summary
                  </h3>
                  <p
                    :class="selected.description ? 'sb-summary' : 'sb-muted'"
                  >
                    {{ selected.description || 'No summary given.' }}
                  </p>
                </section>
              </template>
            </OrganizersDetailPane>
          </template>

          <template #placeholder>
            <GEmptyState
              icon="material-symbols:description-rounded"
              title="Pick a submission"
              description="Its summary and links show here."
              :bordered="false"
            />
          </template>
        </OrganizersListDetail>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.sb {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 88rem;
  margin: 0 auto;
}
.sb-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.sb-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.sb-toolbar__select { flex: 0 1 18rem; min-width: 12rem; }
.sb-toolbar__search { flex: 1 1 18rem; }
.sb-box {
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.sb-box--loading {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
}
.sb-skeleton {
  display: block;
  height: 56px;
}
.sb-list {
  margin: 0;
  padding: 0;
  list-style: none;
  overflow: hidden;
}
@media (min-width: 1024px) {
  .sb-list {
    max-height: calc(100dvh - 8.5rem);
    overflow-y: auto;
  }
}
.sb-list > li { border-top: var(--gh-border-width) solid var(--gh-line-soft); }
.sb-list > li:first-child { border-top: 0; }
.sb-row {
  display: flex;
  flex-direction: column;
  gap: 2px;
  width: 100%;
  padding: 12px 16px;
  border: 0;
  border-left: 4px solid transparent;
  background: transparent;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.sb-row:hover { background: var(--gh-ghost-hover); }
.sb-row:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: -2px;
}
.sb-row.is-selected {
  border-left-color: var(--gh-highlight-edge);
  background: var(--gh-highlight);
  color: var(--gh-highlight-ink);
}
.sb-row.is-selected .sb-row__meta { color: inherit; }
.sb-row__title { font-weight: 700; overflow-wrap: anywhere; }
.sb-row__meta {
  font-size: 14px;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
@media (prefers-reduced-motion: reduce) {
  .sb-row { transition: none; }
}

.sb-facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 12px 24px;
  margin: 0;
}
.sb-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.sb-facts dd {
  margin: 2px 0 0;
  overflow-wrap: anywhere;
}
.sb-link {
  color: inherit;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.sb-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.sb-section__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.sb-summary {
  margin: 0;
  white-space: pre-line;
  overflow-wrap: anywhere;
}
.sb-links {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.sb-tile {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: inherit;
  text-decoration: none;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.sb-tile:hover { background: var(--gh-ghost-hover); }
.sb-tile:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.sb-tile__icon,
.sb-tile__open {
  flex-shrink: 0;
  width: 20px;
  height: 20px;
}
.sb-tile__open { color: var(--gh-text-muted); }
.sb-tile__text {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-width: 0;
}
.sb-tile__label { font-weight: 700; }
.sb-tile__url {
  overflow: hidden;
  font-size: 13px;
  color: var(--gh-text-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}
@media (prefers-reduced-motion: reduce) {
  .sb-tile { transition: none; }
}
</style>
