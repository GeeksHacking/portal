<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryKey,
  geeksHackingPortalApiEndpointsUsersProfileGetEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsUsersProfileUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/**
 * Your profile: the name used on every registration (editable) and the account details that come
 * from GitHub (read-only).
 */
const toast = useGToast()
const queryClient = useQueryClient()
const { data: user, isLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const updateProfileMutation = useGeeksHackingPortalApiEndpointsUsersProfileUpdateEndpoint()

const form = reactive({
  firstName: '',
  lastName: '',
})
const hasPrefilled = ref(false)
// errors show after the first save attempt, then update as you type
const showErrors = ref(false)
const saveError = ref('')

const isValid = computed(() =>
  form.firstName.trim() !== '' && form.lastName.trim() !== '',
)

const isDirty = computed(() =>
  form.firstName.trim() !== (user.value?.firstName ?? '')
  || form.lastName.trim() !== (user.value?.lastName ?? ''),
)

// Fill the form from the account, and again after each save, but never while someone is typing:
// a background refetch of whoami would otherwise wipe their edits.
watch(user, (value) => {
  if (!value || isDirty.value && hasPrefilled.value)
    return

  form.firstName = value.firstName ?? ''
  form.lastName = value.lastName ?? ''
  hasPrefilled.value = true
}, { immediate: true })

const isSubmitting = computed(() => updateProfileMutation.isPending.value)

const firstNameError = computed(() => showErrors.value && !form.firstName.trim() ? 'Enter your first name.' : '')
const lastNameError = computed(() => showErrors.value && !form.lastName.trim() ? 'Enter your last name.' : '')

function resetForm() {
  form.firstName = user.value?.firstName ?? ''
  form.lastName = user.value?.lastName ?? ''
  showErrors.value = false
  saveError.value = ''
}

function handleSubmit(event: Event) {
  saveError.value = ''
  if (!isValid.value) {
    showErrors.value = true
    nextTick(() => (event.target as HTMLFormElement | null)?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus())
    return
  }

  if (!isDirty.value)
    return

  updateProfileMutation.mutate({
    body: {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
    },
  }, {
    onSuccess() {
      showErrors.value = false
      queryClient.invalidateQueries({
        queryKey: geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryKey(),
      })
      queryClient.invalidateQueries({
        queryKey: geeksHackingPortalApiEndpointsUsersProfileGetEndpointQueryKey(),
      })

      toast.add({
        title: 'Profile saved',
        description: 'Your name is updated on your registrations, emails and organiser views.',
        tone: 'ok',
      })
    },
    onError(error) {
      console.error('Failed to update profile', error)
      saveError.value = getApiErrorMessage(error, 'Your profile wasn\'t saved. Try again in a moment.')
    },
  })
}
</script>

<template>
  <GPanel id="profile">
    <template #header>
      <GPanelHeader
        title="Profile"
        as="p"
      />
    </template>

    <template #body>
      <div class="pf">
        <header class="pf-head">
          <h1 class="pf-head__title">
            Your profile
          </h1>
          <p class="pf-head__lead">
            Your name belongs to your account, not to one registration. Changes apply to every hackathon and workshop you join.
          </p>
        </header>

        <GLoading
          v-if="isLoading"
          label="Loading your profile…"
        />

        <GAlert
          v-else-if="!user"
          tone="err"
          title="Couldn't load your profile"
          description="Refresh the page to try again."
        />

        <template v-else>
          <!-- name -->
          <section
            class="pf-card"
            aria-labelledby="pf-name-title"
          >
            <div class="flex flex-col gap-1">
              <h2
                id="pf-name-title"
                class="pf-card__title"
              >
                Your name
              </h2>
              <p class="pf-card__text">
                Organisers see it on registrations and check-in, and it's used in event emails.
              </p>
            </div>

            <form
              class="pf-form"
              novalidate
              @submit.prevent="handleSubmit"
            >
              <div class="pf-form__names">
                <GFormField
                  label="First name"
                  required
                  :error="firstNameError"
                >
                  <GInput
                    v-model="form.firstName"
                    autocomplete="given-name"
                    placeholder="Ada"
                    :invalid="!!firstNameError"
                    required
                  />
                </GFormField>
                <GFormField
                  label="Last name"
                  required
                  :error="lastNameError"
                >
                  <GInput
                    v-model="form.lastName"
                    autocomplete="family-name"
                    placeholder="Lovelace"
                    :invalid="!!lastNameError"
                    required
                  />
                </GFormField>
              </div>

              <GAlert
                v-if="saveError"
                tone="err"
                title="Not saved"
                :description="saveError"
              />

              <div class="pf-form__actions">
                <GButton
                  v-if="isDirty"
                  variant="ghost"
                  label="Discard changes"
                  :disabled="isSubmitting"
                  @click="resetForm"
                />
                <GButton
                  type="submit"
                  variant="primary"
                  icon="material-symbols:save-rounded"
                  label="Save name"
                  :loading="isSubmitting"
                  loading-label="Saving"
                  :disabled="!isDirty"
                />
              </div>
            </form>
          </section>

          <!-- from GitHub -->
          <section
            class="pf-card"
            aria-labelledby="pf-account-title"
          >
            <div class="pf-account">
              <img
                v-if="user.gitHubLogin"
                :src="`https://github.com/${user.gitHubLogin}.png?size=128`"
                alt=""
                class="pf-account__avatar"
                width="56"
                height="56"
              >
              <div class="flex min-w-0 flex-col gap-1">
                <h2
                  id="pf-account-title"
                  class="pf-card__title"
                >
                  Account
                </h2>
                <p class="pf-card__text">
                  These come from your GitHub account. Change them on GitHub.
                </p>
              </div>
            </div>
            <dl class="pf-facts">
              <div>
                <dt>GitHub</dt>
                <dd>
                  <a
                    v-if="user.gitHubLogin"
                    :href="`https://github.com/${user.gitHubLogin}`"
                    target="_blank"
                    rel="noopener"
                    class="pf-link"
                  >@{{ user.gitHubLogin }}<span class="sr-only"> (opens in a new tab)</span></a>
                  <span v-else>Not available</span>
                </dd>
              </div>
              <div>
                <dt>Email</dt>
                <dd>{{ user.email || 'Not shared by GitHub' }}</dd>
              </div>
            </dl>
          </section>
        </template>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.pf {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 42rem;
  margin: 0 auto;
}
.pf-head {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.pf-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
}
.pf-head__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.pf-card {
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
  .pf-card { padding: 24px; }
}
.pf-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
}
.pf-card__text {
  margin: 0;
  color: var(--gh-text-muted);
}
.pf-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.pf-form__names {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
}
@media (min-width: 640px) {
  .pf-form__names { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
.pf-form__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
}
.pf-account {
  display: flex;
  align-items: center;
  gap: 16px;
}
.pf-account__avatar {
  flex: none;
  width: 56px;
  height: 56px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: 999px;
  background: var(--gh-line-soft);
}
.pf-facts {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px 24px;
  margin: 0;
}
@media (min-width: 640px) {
  .pf-facts { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
.pf-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.pf-facts dd {
  margin: 2px 0 0;
  font-weight: 600;
  overflow-wrap: anywhere;
}
.pf-link {
  color: inherit;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.pf-link:hover { text-decoration-thickness: 2px; }
.pf-link:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
  border-radius: 2px;
}
</style>
