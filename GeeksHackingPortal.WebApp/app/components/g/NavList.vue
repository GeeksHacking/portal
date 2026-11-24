<script setup lang="ts">
import type { GNavGroup, GNavItem } from '~/utils/g-nav'

/**
 * GNavList: the sidebar's page links, in labelled groups; replaces the dashboard's
 * vertical UNavigationMenu.
 *
 * - `groups`: `{ label?, items }[]`. An item is `{ label, icon, to, exact? }`, or a parent with
 *   `children` (one level), shown as a disclosure button that starts open when a child is the
 *   current page.
 * - The current page gets the brand's yellow block and `aria-current="page"`. Without `exact`,
 *   an item also counts as current on its sub-pages. A `to` with a query string only matches
 *   when those query values match too.
 * - `collapsed` (the shell's icon rail): icons only, each still named for screen readers and
 *   on hover; group labels are hidden and a parent links straight to its first child.
 */
const props = defineProps<{
  groups: GNavGroup[]
  collapsed?: boolean
  label?: string
}>()

const route = useRoute()
function isActive(item: GNavItem): boolean {
  if (item.children)
    return item.children.some(isActive)
  if (!item.to)
    return false
  const [toPath = '', toQuery] = item.to.split('?')
  if (toQuery && [...new URLSearchParams(toQuery)].some(([k, v]) => route.query[k] !== v))
    return false
  const path = route.path.replace(/\/$/, '') || '/'
  return item.exact || toQuery ? path === toPath : path === toPath || path.startsWith(`${toPath}/`)
}

const uid = useId()
const openParents = ref<Record<string, boolean>>({})
function keyOf(groupIndex: number, item: GNavItem) {
  return `${groupIndex}-${item.label}`
}
function isOpen(groupIndex: number, item: GNavItem) {
  return openParents.value[keyOf(groupIndex, item)] ?? isActive(item)
}
function toggle(groupIndex: number, item: GNavItem) {
  openParents.value[keyOf(groupIndex, item)] = !isOpen(groupIndex, item)
}

const visibleGroups = computed(() => props.groups.filter(g => g.items.length))
</script>

<template>
  <nav
    class="g-nav"
    :class="{ 'g-nav--collapsed': collapsed }"
    :aria-label="label ?? 'Pages'"
  >
    <div
      v-for="(group, gi) in visibleGroups"
      :key="gi"
      class="g-nav__group"
    >
      <p
        v-if="group.label && !collapsed"
        :id="`${uid}-g${gi}`"
        class="g-nav__group-label"
      >
        {{ group.label }}
      </p>
      <ul
        class="g-nav__list"
        :aria-labelledby="group.label && !collapsed ? `${uid}-g${gi}` : undefined"
      >
        <li
          v-for="item in group.items"
          :key="item.label"
        >
          <!-- parent with children -->
          <template v-if="item.children?.length">
            <NuxtLink
              v-if="collapsed"
              :to="item.children[0]!.to"
              class="g-nav__link"
              :aria-current="isActive(item) ? 'page' : undefined"
              :aria-label="item.label"
              :title="item.label"
            >
              <Icon
                v-if="item.icon"
                :name="item.icon"
                class="g-nav__icon"
                aria-hidden="true"
              />
            </NuxtLink>
            <template v-else>
              <button
                type="button"
                class="g-nav__link g-nav__parent"
                :class="{ 'g-nav__parent--active': isActive(item) }"
                :aria-expanded="isOpen(gi, item)"
                :aria-controls="`${uid}-${gi}-${item.label}`"
                @click="toggle(gi, item)"
              >
                <Icon
                  v-if="item.icon"
                  :name="item.icon"
                  class="g-nav__icon"
                  aria-hidden="true"
                />
                <span class="g-nav__label">{{ item.label }}</span>
                <Icon
                  name="material-symbols:keyboard-arrow-down-rounded"
                  class="g-nav__chevron"
                  aria-hidden="true"
                />
              </button>
              <ul
                v-show="isOpen(gi, item)"
                :id="`${uid}-${gi}-${item.label}`"
                class="g-nav__children"
              >
                <li
                  v-for="child in item.children"
                  :key="child.label"
                >
                  <NuxtLink
                    :to="child.to"
                    class="g-nav__link g-nav__child"
                    :aria-current="isActive(child) ? 'page' : undefined"
                  >
                    <span class="g-nav__label">{{ child.label }}</span>
                  </NuxtLink>
                </li>
              </ul>
            </template>
          </template>

          <!-- plain link -->
          <NuxtLink
            v-else
            :to="item.to"
            class="g-nav__link"
            :aria-current="isActive(item) ? 'page' : undefined"
            :aria-label="collapsed ? item.label : undefined"
            :title="collapsed ? item.label : undefined"
          >
            <Icon
              v-if="item.icon"
              :name="item.icon"
              class="g-nav__icon"
              aria-hidden="true"
            />
            <span
              v-if="!collapsed"
              class="g-nav__label"
            >{{ item.label }}</span>
          </NuxtLink>
        </li>
      </ul>
    </div>
  </nav>
</template>

<style scoped>
.g-nav {
  display: flex;
  flex-direction: column;
  gap: 18px;
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.g-nav__group-label {
  margin: 0 0 6px;
  padding: 0 10px;
  font-family: var(--font-display);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.g-nav__list,
.g-nav__children {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.g-nav__children {
  margin: 2px 0 4px 21px;
  padding-left: 10px;
  border-left: var(--gh-border-width) solid var(--gh-line-soft);
}

.g-nav__link {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  min-height: 40px;
  padding: 0 10px;
  border: var(--gh-border-width) solid transparent;
  border-radius: var(--radius-gh-sm);
  background: none;
  color: var(--gh-text);
  font: inherit;
  font-size: 14px;
  font-weight: 600;
  text-align: left;
  text-decoration: none;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.g-nav__link:hover {
  background: var(--gh-ghost-hover);
}
.g-nav__link:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 1px;
}
.g-nav__child {
  min-height: 34px;
  font-weight: 500;
}
.g-nav__icon {
  flex: none;
  width: 20px;
  height: 20px;
}
.g-nav__label {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.g-nav__chevron {
  flex: none;
  width: 18px;
  height: 18px;
  color: var(--gh-text-muted);
  transition: transform var(--gh-dur-fast) var(--ease-gh);
}
.g-nav__parent[aria-expanded='false'] .g-nav__chevron {
  transform: rotate(-90deg);
}
@media (prefers-reduced-motion: reduce) {
  .g-nav__chevron { transition: none; }
}

/* the current page: the brand's yellow block (a solid fill with dark text, dark edge) */
.g-nav__link[aria-current='page'] {
  border-color: var(--gh-on-accent);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
/* a closed parent whose child is current gets a quieter marker */
.g-nav__parent--active[aria-expanded='false'] {
  border-color: var(--gh-line);
}

.g-nav--collapsed .g-nav__link {
  justify-content: center;
  padding: 0;
}
</style>
