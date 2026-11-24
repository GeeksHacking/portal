<script setup lang="ts">
/** One section of a stats page: heading, a line saying what it covers, then its figures. */
defineProps<{
  title: string
  description?: string
  id?: string
}>()
</script>

<template>
  <section
    class="osc"
    :aria-labelledby="id ? `${id}-title` : undefined"
  >
    <div class="osc__head">
      <div class="osc__titles">
        <h2
          :id="id ? `${id}-title` : undefined"
          class="osc__title"
        >
          {{ title }}
        </h2>
        <p
          v-if="description"
          class="osc__desc"
        >
          {{ description }}
        </p>
      </div>
      <div
        v-if="$slots.actions"
        class="osc__actions"
      >
        <slot name="actions" />
      </div>
    </div>
    <div class="osc__body">
      <slot />
    </div>
  </section>
</template>

<style scoped>
.osc {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.osc__head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}
.osc__titles {
  display: flex;
  flex: 1 1 16rem;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}
.osc__title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.osc__desc {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.osc__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 8px;
}
.osc__body {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;
}
</style>
