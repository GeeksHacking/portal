<script setup lang="ts">
/**
 * AuthFrame: the frame for signed-out first screens (/login, the registration landing).
 * A solid-yellow brand panel with the guide's on-yellow logo and tagline, beside the content
 * from 1024px and as a band above it on narrower screens. Content goes in the default slot.
 */
withDefaults(defineProps<{
  tagline?: string
}>(), {
  tagline: 'Giving · Geeks · Grow',
})
</script>

<template>
  <div class="auth-frame">
    <aside
      class="auth-frame__brand"
      aria-label="GeeksHacking"
    >
      <GLogo
        on="yellow"
        size="xl"
        class="auth-frame__logo"
      />
      <p class="auth-frame__tagline">
        {{ tagline }}
      </p>
    </aside>
    <main class="auth-frame__main">
      <div class="auth-frame__content">
        <slot />
      </div>
    </main>
  </div>
</template>

<style scoped>
.auth-frame {
  display: flex;
  flex-direction: column;
  min-height: 100dvh;
  background: var(--gh-bg);
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}

.auth-frame__brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 32px 24px;
  border-bottom: var(--gh-border-width) solid var(--color-gh-ink);
  background: var(--color-gh-yellow);
  color: var(--color-gh-ink);
}
.auth-frame__logo {
  --g-logo-height: clamp(88px, 22vw, 120px);
}
.auth-frame__tagline {
  margin: 0;
  font-family: var(--font-display);
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.auth-frame__main {
  display: flex;
  flex: 1;
  align-items: center;
  justify-content: center;
  padding: 40px 20px 56px;
}
.auth-frame__content {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 420px;
}

@media (min-width: 1024px) {
  .auth-frame {
    flex-direction: row;
  }
  .auth-frame__brand {
    flex: 0 0 44%;
    gap: 24px;
    border-right: var(--gh-border-width) solid var(--color-gh-ink);
    border-bottom: 0;
  }
  .auth-frame__logo {
    --g-logo-height: clamp(150px, 15vw, 210px);
  }
  .auth-frame__tagline {
    font-size: 15px;
  }
}
</style>
