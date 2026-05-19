<script setup lang="ts">
import type { User } from 'firebase/auth'
import {
  getAdditionalUserInfo,
  getAuth,
  getRedirectResult,
  OAuthProvider,
  signInWithRedirect,
} from 'firebase/auth'

const user = ref<User | null>(null)
const isLoading = ref(true)
const error = ref<string | null>(null)
const profile = ref<unknown>(null)
const accessToken = ref<string | undefined>()
const idToken = ref<string | undefined>()
const diagnostics = ref<Record<string, unknown>>({})
const auth = getAuth(useNuxtApp().$firebase)

auth.onAuthStateChanged((u) => {
  user.value = u
  if (u !== null)
    sessionStorage.removeItem('oidc.portal.redirectStarted')
})

const provider = new OAuthProvider('oidc.portal')
provider.addScope('openid')
provider.addScope('profile')
provider.addScope('email')

async function startSignIn() {
  error.value = null
  sessionStorage.setItem('oidc.portal.redirectStarted', 'true')
  await signInWithRedirect(auth, provider)
}

function readRedirectDiagnostics() {
  const params = new URLSearchParams(window.location.search)
  const hashParams = new URLSearchParams(window.location.hash.replace(/^#/, ''))

  diagnostics.value = {
    href: window.location.href,
    search: Object.fromEntries(params),
    hash: Object.fromEntries(hashParams),
    redirectStarted: sessionStorage.getItem('oidc.portal.redirectStarted'),
    firebaseAuthEvent: sessionStorage.getItem(
      `firebase:redirectEvent:${auth.app.options.apiKey}:${auth.name}`,
    ),
  }

  return { params, hashParams }
}

function resetSignInState() {
  sessionStorage.removeItem('oidc.portal.redirectStarted')
  error.value = null
}

onMounted(async () => {
  try {
    const { params, hashParams } = readRedirectDiagnostics()
    const result = await getRedirectResult(auth)

    if (result !== null) {
      user.value = result.user
      profile.value = getAdditionalUserInfo(result)?.profile ?? null

      const credential = OAuthProvider.credentialFromResult(result)
      accessToken.value = credential?.accessToken
      idToken.value = credential?.idToken

      sessionStorage.removeItem('oidc.portal.redirectStarted')
      return
    }

    if (auth.currentUser !== null) {
      user.value = auth.currentUser
      sessionStorage.removeItem('oidc.portal.redirectStarted')
      return
    }

    if (sessionStorage.getItem('oidc.portal.redirectStarted') === 'true') {
      const firebaseError = params.get('firebaseError') ?? hashParams.get('firebaseError')
      const oauthError = params.get('error') ?? hashParams.get('error')
      const oauthErrorDescription = params.get('error_description') ?? hashParams.get('error_description')

      error.value = [
        'Sign-in returned without a Firebase user.',
        firebaseError ? `Firebase error: ${firebaseError}` : null,
        oauthError ? `OAuth error: ${oauthError}` : null,
        oauthErrorDescription ? `OAuth description: ${oauthErrorDescription}` : null,
        'Check that Firebase uses a Web/confidential OpenIddict client, the Firebase auth handler redirect URI is registered, and the client secret matches.',
      ]
        .filter(Boolean)
      .join(' ')
      return
    }

    await startSignIn()
  }
  catch (err) {
    sessionStorage.removeItem('oidc.portal.redirectStarted')
    error.value = err instanceof Error ? err.message : 'Sign-in failed.'
  }
  finally {
    isLoading.value = false
  }
})
</script>
<template>
  <div>
    <p v-if="isLoading">
      Signing in...
    </p>
    <p v-else-if="error">
      {{ error }}
    </p>
    <template v-else>
      <button v-if="!user" type="button" @click="startSignIn">
        Sign in
      </button>

      <section v-if="user">
        <h2>User</h2>
        <pre>{{ JSON.stringify(user, null, 2) }}</pre>
      </section>

      <section v-if="profile">
        <h2>IdP profile</h2>
        <pre>{{ JSON.stringify(profile, null, 2) }}</pre>
      </section>

      <section v-if="accessToken || idToken">
        <h2>Credential</h2>
        <pre>{{ JSON.stringify({ accessToken, idToken }, null, 2) }}</pre>
      </section>
    </template>

    <section>
      <h2>Diagnostics</h2>
      <button type="button" @click="resetSignInState">
        Reset sign-in state
      </button>
      <pre>{{ JSON.stringify(diagnostics, null, 2) }}</pre>
    </section>
  </div>
</template>
