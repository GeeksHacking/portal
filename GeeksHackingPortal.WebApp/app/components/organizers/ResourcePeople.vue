<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsParticipantBreakdownItem } from '@geekshacking/portal-sdk'

/** People who redeemed resources: count, which ones, first/latest, and each redemption on demand. */
defineProps<{
  people: GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsParticipantBreakdownItem[]
  multiResource: boolean
}>()

const when = (value: string | null | undefined) => (value ? formatOrganizerDateTime(value) : '–')
</script>

<template>
  <div class="rp">
    <table class="rp-table">
      <thead>
        <tr>
          <th scope="col">
            Name
          </th>
          <th
            scope="col"
            class="rp-num"
          >
            Redeemed
          </th>
          <th
            v-if="multiResource"
            scope="col"
          >
            Resources
          </th>
          <th scope="col">
            Latest
          </th>
          <th scope="col">
            Every redemption
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="person in people"
          :key="person.participantId ?? person.userId"
        >
          <th scope="row">
            {{ person.userName || 'Unknown' }}
          </th>
          <td class="rp-num">
            {{ (person.redemptionCount ?? 0).toLocaleString() }}
          </td>
          <td v-if="multiResource">
            {{ [...new Set((person.redemptions ?? []).map(r => r.resourceName))].join(', ') }}
          </td>
          <td>{{ when(person.lastRedeemedAt) }}</td>
          <td>
            <details class="rp-details">
              <summary>{{ (person.redemptions ?? []).length === 1 ? '1 time' : `${(person.redemptions ?? []).length} times` }}</summary>
              <ol class="rp-times">
                <li
                  v-for="r in person.redemptions"
                  :key="r.redemptionId"
                >
                  <template v-if="multiResource">
                    {{ r.resourceName }} ·
                  </template>{{ when(r.timestamp) }}
                </li>
              </ol>
            </details>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.rp {
  overflow-x: auto;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
}
.rp-table {
  width: 100%;
  min-width: 36rem;
  border-collapse: collapse;
  font-size: 14px;
}
.rp-table th,
.rp-table td {
  padding: 8px 12px;
  border-top: 1px solid var(--gh-line-soft);
  text-align: left;
  vertical-align: top;
}
.rp-table thead th {
  border-top: 0;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.rp-table tbody th { font-weight: 600; }
.rp-num {
  font-variant-numeric: tabular-nums;
  text-align: right !important;
}
.rp-details summary {
  width: fit-content;
  cursor: pointer;
}
.rp-details summary:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.rp-times {
  margin: 6px 0 0;
  padding-left: 18px;
  color: var(--gh-text-muted);
}
</style>
