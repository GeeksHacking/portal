<script setup lang="ts">
import type { GNavGroup, GNavItem } from '~/utils/g-nav'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { CONTACT_EMAIL, SOCIAL_LINKS, SPONSOR_EMAIL } from '~/constants/socials'

/**
 * The signed-in portal: one sidebar for everything (Explore, the event you're taking part in,
 * organizer tools, admin) around the current page. Used by /dash/** (via pages/dash.vue) and the
 * participant event pages (/[shortCode]/team, challenges, submission, schedule).
 */
useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking` : 'GeeksHacking'),
})

const { data: user, isLoading: userIsLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()

const { data: organizerHackathons } = useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint({
  query: { enabled: computed(() => !!user.value?.id) },
})
const { data: organizerWorkshops } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint({
  query: { enabled: computed(() => !!user.value?.id) },
})
const isOrganizer = computed(() =>
  (organizerHackathons.value?.hackathons?.length ?? 0) > 0
  || (organizerWorkshops.value?.standaloneWorkshops?.length ?? 0) > 0,
)

const route = useRoute()
const hackathonId = computed(() => route.params.hackathonId as string | undefined)
const standaloneWorkshopId = computed(() => route.params.standaloneWorkshopId as string | undefined)
const isDashRoute = computed(() => route.path === '/dash' || route.path.startsWith('/dash/'))
// the participant event pages live at /[shortCode]/…, outside /dash
const eventCode = computed(() => (!isDashRoute.value ? hackathonId.value : undefined))

const { data: eventHackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint(
  { path: computed(() => ({ hackathonIdOrShortCode: eventCode.value ?? '' })) },
  { query: { enabled: computed(() => !!eventCode.value) } },
)

const groups = computed<GNavGroup[]>(() => {
  const result: GNavGroup[] = [
    { items: [{ label: 'Explore', icon: 'material-symbols:explore-rounded', exact: true, to: '/dash' }] },
  ]

  if (eventCode.value) {
    const base = `/${eventCode.value}`
    result.push({
      label: eventHackathon.value?.name ?? 'This event',
      items: [
        { label: 'Team', icon: 'material-symbols:groups-rounded', to: `${base}/team` },
        { label: 'Challenges', icon: 'material-symbols:trophy-rounded', to: `${base}/challenges` },
        { label: 'Submission', icon: 'material-symbols:upload-file-rounded', to: `${base}/submission` },
        { label: 'Schedule', icon: 'material-symbols:calendar-month-rounded', to: `${base}/schedule` },
      ],
    })
  }

  const isParticipantView = route.path.includes('/participant/') || route.path.endsWith('/participant') || route.path.includes('/registration')

  if (user.value?.id && isOrganizer.value) {
    const items: GNavItem[] = [
      { label: 'Workspace', icon: 'material-symbols:dashboard-rounded', exact: true, to: '/dash/manage' },
    ]

    if (standaloneWorkshopId.value) {
      const base = `/dash/standalone/${standaloneWorkshopId.value}`
      items.push(
        { label: 'Check Ins', icon: 'material-symbols:qr-code-2-rounded', to: `${base}/checkin` },
        { label: 'Resources', icon: 'material-symbols:redeem-rounded', to: `${base}/resources` },
        { label: 'Stats', icon: 'material-symbols:pie-chart-rounded', to: `${base}/stats` },
        { label: 'Participants', icon: 'material-symbols:group-rounded', to: `${base}/participants` },
        { label: 'Organizers', icon: 'material-symbols:manage-accounts-rounded', to: `${base}/organizers` },
        { label: 'Data Export', icon: 'material-symbols:download-rounded', to: `${base}/infopack` },
        { label: 'Questions', icon: 'material-symbols:help-rounded', to: `${base}/questions` },
        { label: 'Settings', icon: 'material-symbols:settings-rounded', to: `${base}/settings` },
      )
    }
    else if (isDashRoute.value && hackathonId.value && !isParticipantView) {
      const base = `/dash/${hackathonId.value}`
      items.push(
        { label: 'Check Ins', icon: 'material-symbols:qr-code-2-rounded', to: `${base}/checkin` },
        { label: 'Resources', icon: 'material-symbols:redeem-rounded', to: `${base}/resources` },
        { label: 'Stats', icon: 'material-symbols:pie-chart-rounded', to: `${base}/stats` },
        { label: 'Participants', icon: 'material-symbols:group-rounded', to: `${base}/participants` },
        { label: 'Teams', icon: 'material-symbols:group-add-rounded', to: `${base}/teams` },
        {
          label: 'Challenges',
          icon: 'material-symbols:trophy-rounded',
          children: [
            { label: 'Challenges', to: `${base}/challenges` },
            { label: 'Analytics', to: `${base}/challenge-dashboard` },
          ],
        },
        { label: 'Submissions', icon: 'material-symbols:description-rounded', to: `${base}/submissions` },
        { label: 'Judges', icon: 'material-symbols:balance-rounded', to: `${base}/judges` },
        { label: 'Organizers', icon: 'material-symbols:manage-accounts-rounded', to: `${base}/organizers` },
        { label: 'Questions', icon: 'material-symbols:help-rounded', to: `${base}/questions` },
        { label: 'Settings', icon: 'material-symbols:settings-rounded', to: `${base}/settings` },
        { label: 'Data Export', icon: 'material-symbols:download-rounded', to: `${base}/infopack` },
      )
    }

    result.push({ label: 'Organizer', items })
  }

  if (user.value?.isRoot) {
    result.push({
      label: 'Admin',
      items: [
        { label: 'My OAuth Apps', icon: 'material-symbols:key-rounded', to: '/dash/oauth-applications' },
        { label: 'All OAuth Apps', icon: 'material-symbols:apps-rounded', to: '/dash/admin/oauth-applications' },
      ],
    })
  }

  return result
})

const contactOpen = ref(false)
</script>

<template>
  <GShell label="Portal navigation">
    <template #header="{ collapsed }">
      <NuxtLink
        to="/dash"
        class="portal-brand"
        :class="{ 'portal-brand--collapsed': collapsed }"
        :aria-label="collapsed ? 'GeeksHacking Portal home' : undefined"
      >
        <GLogo
          :wordmark="false"
          size="sm"
          decorative
        />
        <span
          v-if="!collapsed"
          class="portal-brand__name"
        >GeeksHacking <span class="portal-brand__sub">Portal</span></span>
      </NuxtLink>
    </template>

    <template #default="{ collapsed }">
      <GNavList
        :groups="groups"
        :collapsed="collapsed"
        label="Portal pages"
      />
    </template>

    <template #footer="{ collapsed }">
      <div
        class="portal-footer"
        :class="{ 'portal-footer--collapsed': collapsed }"
      >
        <GSkeleton
          v-if="userIsLoading"
          class="h-10 w-full"
        />
        <NuxtLink
          v-else
          to="/dash/profile"
          class="portal-account"
          :aria-current="route.path === '/dash/profile' ? 'page' : undefined"
          :aria-label="collapsed ? `Profile (${user?.gitHubLogin ?? 'you'})` : undefined"
          :title="collapsed ? 'Profile' : undefined"
        >
          <img
            v-if="user?.gitHubLogin"
            :src="`https://github.com/${user.gitHubLogin}.png?size=64`"
            alt=""
            class="portal-account__avatar"
          >
          <Icon
            v-else
            name="material-symbols:person-rounded"
            class="portal-account__avatar"
            aria-hidden="true"
          />
          <span
            v-if="!collapsed"
            class="portal-account__text"
          >
            <span class="portal-account__login">{{ user?.gitHubLogin ?? 'Profile' }}</span>
            <span class="portal-account__hint">Profile</span>
          </span>
        </NuxtLink>
        <GButton
          variant="ghost"
          size="sm"
          icon="material-symbols:support-agent-rounded"
          :label="collapsed ? undefined : 'Contact us'"
          :aria-label="collapsed ? 'Contact us' : undefined"
          :block="!collapsed"
          class="portal-footer__contact"
          @click="contactOpen = true"
        />
      </div>
    </template>

    <template #main>
      <slot />
    </template>
  </GShell>

  <GModal
    v-model:open="contactOpen"
    title="Contact us"
    description="We usually reply within a couple of days."
    size="sm"
  >
    <div class="flex flex-col gap-5">
      <div class="flex flex-col gap-1">
        <GLabelStrip
          as="h3"
          size="sm"
          label="General enquiries"
        />
        <p class="gh-small text-gh-muted">
          Email
          <a
            class="portal-link"
            :href="`mailto:${CONTACT_EMAIL}`"
          >{{ CONTACT_EMAIL }}</a>
          or message us on Facebook.
        </p>
      </div>
      <div class="flex flex-col gap-1">
        <GLabelStrip
          as="h3"
          size="sm"
          label="Sponsorships"
        />
        <p class="gh-small text-gh-muted">
          Email
          <a
            class="portal-link"
            :href="`mailto:${SPONSOR_EMAIL}`"
          >{{ SPONSOR_EMAIL }}</a>.
        </p>
      </div>
      <ul class="flex gap-2">
        <li
          v-for="social in SOCIAL_LINKS"
          :key="social.name"
        >
          <GButton
            :href="social.url"
            target="_blank"
            variant="secondary"
            size="sm"
            :icon="social.icon"
            :aria-label="`${social.name} (opens in a new tab)`"
          />
        </li>
      </ul>
    </div>
  </GModal>
</template>

<style scoped>
.portal-brand {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  border-radius: var(--radius-gh-sm);
  color: var(--gh-text);
  text-decoration: none;
}
.portal-brand--collapsed {
  margin: 0 auto;
}
.portal-brand:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.portal-brand__name {
  display: flex;
  flex-direction: column;
  min-width: 0;
  font-family: var(--font-display);
  font-size: 16px;
  font-weight: 700;
  line-height: 1.15;
  white-space: nowrap;
}
/* "Portal" on its own line so the name never gets cut off next to the mark */
.portal-brand__sub {
  font-family: var(--font-body);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}

.portal-footer {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.portal-footer--collapsed {
  align-items: center;
}
.portal-account {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 48px;
  padding: 4px 8px;
  border: var(--gh-border-width) solid transparent;
  border-radius: var(--radius-gh-sm);
  color: var(--gh-text);
  text-decoration: none;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.portal-footer--collapsed .portal-account {
  padding: 4px;
}
.portal-account:hover {
  background: var(--gh-ghost-hover);
}
.portal-account:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 1px;
}
.portal-account[aria-current='page'] {
  border-color: var(--gh-line);
}
.portal-account__avatar {
  flex: none;
  width: 32px;
  height: 32px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: 999px;
  background: var(--gh-surface-sunken);
}
.portal-account__text {
  display: flex;
  flex-direction: column;
  min-width: 0;
  line-height: 1.25;
}
.portal-account__login {
  overflow: hidden;
  font-family: var(--font-mono);
  font-size: 13px;
  letter-spacing: 0;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.portal-account__hint {
  font-size: 12px;
  color: var(--gh-text-muted);
}
.portal-footer .portal-footer__contact {
  justify-content: flex-start;
}
.portal-footer--collapsed .portal-footer__contact {
  justify-content: center;
}
.portal-link {
  color: var(--gh-text);
  font-weight: 600;
  text-decoration: underline;
  text-decoration-color: var(--gh-accent);
  text-decoration-thickness: 2px;
  text-underline-offset: 3px;
}
</style>
