<script setup lang="ts">
/**
 * GLogo: the GeeksHacking glasses logo.
 *
 * The glasses are traced from the designers' brand-guide moodboard (public/references/1-*.png)
 * because no vector files exist yet. Swap FRAME_PATH for the real path when the designers export one.
 *
 * - `wordmark` (default true) primary logo with the "GeeksHacking" bar; false = secondary (glasses only).
 * - `variant`  color (default): frame + hard offset shadow; mono: one colour, frame and shadow merged.
 * - `on`       auto (default) follows the light/dark theme; light / dark / yellow force a version for
 *              that background (e.g. `on="yellow"` on the yellow hero).
 * - `clearance` pads the logo by the guide's clearance space (x on every side).
 * - `size`     sets the height: sm 28 · md 40 · lg 64 · xl 120px. The wordmark is only legible from lg
 *              up, so smaller placements (nav bars, footers) use the glasses alone and set the name
 *              as real text beside them.
 *
 * Brand rules (guide slide 1-5): keep it upright, keep its proportions, never recolour it. That's why
 * there's no colour prop and the size only sets the height.
 * It's an image with an accessible name; pass `decorative` when the brand name is already next to it.
 */
const props = withDefaults(defineProps<{
  wordmark?: boolean
  variant?: 'color' | 'mono'
  on?: 'auto' | 'light' | 'dark' | 'yellow'
  size?: 'sm' | 'md' | 'lg' | 'xl'
  clearance?: boolean
  decorative?: boolean
  title?: string
}>(), {
  wordmark: true,
  variant: 'color',
  on: 'auto',
  size: 'md',
  title: 'GeeksHacking',
})

// traced frame: outer outline + two lens holes (even-odd), in a 430.8 × 177 box
const FRAME_PATH = 'M383.42 0.19C386.32 -0.20 389.29 -0.10 392.20 0.10C394.38 0.26 396.57 0.72 398.70 1.27C400.59 1.75 402.50 2.33 404.24 3.19C407.16 4.64 410.13 6.20 412.70 8.19C415.37 10.27 417.77 12.81 419.95 15.40C421.48 17.23 422.70 19.35 423.85 21.44C424.70 22.99 425.42 24.64 425.95 26.32C426.77 28.97 427.66 31.67 427.90 34.44C429.28 50.55 430.19 66.79 430.79 82.94C430.88 85.21 430.35 87.47 429.95 89.69C429.71 91.05 429.36 92.41 428.87 93.69C427.99 95.99 426.94 98.24 425.83 100.44C425.26 101.58 424.56 102.65 423.84 103.69C422.84 105.15 421.83 106.60 420.70 107.94C419.51 109.35 418.18 110.64 416.88 111.94C415.85 112.97 414.83 114.02 413.70 114.92C411.47 116.69 409.21 118.48 406.77 119.94C403.65 121.82 400.35 123.46 397.03 124.94C394.41 126.11 391.69 127.08 388.95 127.92C386.33 128.73 383.66 129.45 380.95 129.90C365.66 132.46 350.31 134.84 334.95 136.96C328.81 137.80 322.62 138.35 316.45 138.77C313.62 138.96 310.78 138.90 307.95 138.78C304.53 138.62 301.09 138.41 297.70 137.92C294.25 137.43 290.79 136.81 287.45 135.87C284.79 135.12 282.14 134.12 279.70 132.84C277.12 131.48 274.70 129.75 272.40 127.94C270.28 126.28 268.35 124.36 266.45 122.44C264.96 120.94 263.59 119.32 262.23 117.69C261.17 116.42 260.11 115.13 259.20 113.75C257.49 111.13 255.81 108.47 254.37 105.69C252.17 101.45 250.10 97.11 248.29 92.69C246.71 88.86 245.33 84.93 244.19 80.94C241.63 72.01 239.14 63.01 237.17 53.94C236.59 51.26 236.54 48.44 236.56 45.69C236.57 43.94 236.86 42.15 237.26 40.44C237.59 39.07 240.02 36.38 238.75 36.44C223.06 37.29 203.81 40.79 186.39 43.19C184.63 43.44 181.43 43.43 181.20 44.38C180.92 45.51 183.82 47.66 184.86 49.44C185.72 50.93 186.35 52.57 186.91 54.19C187.39 55.57 187.78 57.00 187.99 58.44C188.34 60.84 188.44 63.27 188.58 65.69C188.97 72.11 189.36 78.53 189.58 84.94C189.69 87.78 189.67 90.61 189.57 93.44C189.42 97.53 189.27 101.63 188.82 105.69C188.36 109.88 187.67 114.06 186.86 118.19C186.38 120.64 185.73 123.08 184.95 125.44C184.05 128.16 183.10 130.89 181.82 133.44C180.06 136.97 178.05 140.43 175.84 143.69C174.34 145.91 172.58 148.00 170.70 149.89C169.21 151.38 167.47 152.68 165.70 153.84C162.89 155.67 159.93 157.32 156.95 158.86C154.68 160.03 152.33 161.03 149.95 161.94C147.04 163.06 144.08 164.11 141.07 164.94C135.91 166.37 130.72 167.78 125.45 168.71C109.85 171.46 94.13 173.71 78.45 175.97C75.05 176.46 71.62 176.83 68.20 176.98C65.04 177.12 61.85 177.07 58.70 176.86C56.19 176.69 53.67 176.31 51.20 175.83C48.42 175.30 45.63 174.74 42.95 173.85C39.71 172.78 36.55 171.38 33.45 169.96C32.26 169.41 31.16 168.67 30.07 167.94C28.14 166.67 26.16 165.43 24.40 163.94C22.54 162.37 20.83 160.58 19.20 158.76C18.09 157.52 17.11 156.16 16.20 154.76C15.14 153.14 14.04 151.48 13.29 149.69C12.37 147.52 11.78 145.18 11.20 142.87C7.44 128.01 3.35 113.16 0.27 98.19C-0.35 95.18 -0.09 92.01 0.09 88.94C0.22 86.84 0.67 84.74 1.20 82.69C1.71 80.72 2.30 78.73 3.20 76.90C4.64 73.96 6.20 70.97 8.20 68.40C10.27 65.74 12.76 63.28 15.41 61.19C17.92 59.22 20.82 57.64 23.67 56.19C25.33 55.35 27.16 54.83 28.95 54.32C30.66 53.84 32.43 53.51 34.20 53.24C70.43 47.70 106.76 42.62 142.95 36.91C144.60 36.65 146.05 35.60 147.70 35.34C185.22 29.36 222.86 23.88 260.45 18.19C262.78 17.84 265.12 17.56 267.45 17.22C306.11 11.56 344.73 5.50 383.42 0.19ZM385.03 10.19C387.48 9.84 390.00 10.06 392.45 10.27C393.87 10.39 395.29 10.77 396.66 11.19C398.40 11.74 400.14 12.41 401.79 13.19C402.98 13.76 404.10 14.51 405.20 15.26C406.15 15.91 407.10 16.59 407.95 17.38C409.32 18.65 410.69 19.98 411.84 21.44C413.00 22.91 414.01 24.53 414.87 26.19C415.73 27.87 416.52 29.63 416.99 31.44C417.57 33.71 417.85 36.09 418.01 38.44C419.04 53.51 419.97 68.62 420.56 83.69C420.62 85.29 420.26 86.89 419.94 88.44C419.72 89.55 419.38 90.66 418.93 91.69C418.05 93.74 417.09 95.78 415.95 97.69C415.10 99.13 414.07 100.49 412.95 101.74C411.31 103.57 409.56 105.32 407.70 106.92C405.98 108.39 404.11 109.72 402.20 110.93C399.87 112.40 397.47 113.79 394.97 114.94C392.41 116.13 389.71 117.09 387.02 117.94C384.54 118.73 382.01 119.45 379.45 119.88C364.24 122.45 348.98 124.85 333.70 126.94C327.31 127.82 320.87 128.61 314.45 128.78C309.12 128.93 303.71 128.63 298.45 127.90C294.38 127.33 290.24 126.38 286.45 124.87C283.56 123.73 280.85 121.88 278.41 119.94C275.83 117.91 273.60 115.39 271.39 112.94C269.90 111.30 268.56 109.51 267.30 107.69C266.19 106.10 265.21 104.41 264.29 102.69C262.53 99.41 260.86 96.07 259.28 92.69C258.52 91.07 257.88 89.38 257.28 87.69C255.86 83.72 254.37 79.75 253.20 75.69C251.03 68.09 248.99 60.40 247.26 52.69C246.84 50.82 246.75 48.86 246.75 46.94C246.76 45.44 246.86 43.88 247.27 42.44C247.67 41.00 248.38 39.57 249.20 38.31C249.94 37.16 250.97 36.16 251.96 35.19C252.70 34.46 253.52 33.76 254.40 33.19C255.58 32.43 256.83 31.70 258.15 31.19C261.18 30.03 264.24 28.73 267.45 28.19C306.53 21.73 345.81 15.82 385.03 10.19ZM155.45 45.69C158.11 45.57 160.81 45.83 163.45 46.16C164.78 46.33 166.12 46.69 167.35 47.19C168.78 47.78 170.20 48.53 171.45 49.44C172.65 50.33 173.79 51.40 174.70 52.58C175.59 53.74 176.27 55.10 176.87 56.44C177.36 57.56 177.81 58.74 177.95 59.94C178.52 64.74 178.79 69.60 179.00 74.44C179.27 80.69 179.40 86.95 179.38 93.19C179.37 96.70 179.20 100.21 178.91 103.69C178.68 106.37 178.30 109.05 177.82 111.69C177.29 114.63 176.65 117.56 175.88 120.44C175.36 122.40 174.70 124.32 173.95 126.19C173.06 128.41 172.14 130.66 170.95 132.72C169.44 135.32 167.75 137.88 165.83 140.19C164.41 141.91 162.70 143.42 160.95 144.79C159.41 146.00 157.67 146.99 155.95 147.94C153.42 149.32 150.85 150.65 148.20 151.78C145.51 152.93 142.73 153.89 139.95 154.79C137.41 155.61 134.83 156.36 132.22 156.94C127.41 158.02 122.56 158.96 117.70 159.76C104.64 161.88 91.55 163.83 78.45 165.70C74.80 166.22 71.12 166.66 67.45 166.92C65.46 167.06 63.44 167.04 61.45 166.91C58.61 166.72 55.76 166.40 52.95 165.98C51.26 165.72 49.59 165.35 47.95 164.89C45.84 164.30 43.68 163.75 41.70 162.83C38.68 161.41 35.80 159.63 32.95 157.87C31.96 157.27 31.04 156.54 30.20 155.76C28.83 154.48 27.47 153.16 26.31 151.69C25.14 150.22 24.02 148.63 23.20 146.94C22.33 145.13 21.74 143.16 21.24 141.19C17.39 126.07 13.30 110.92 10.15 95.69C9.66 93.34 9.95 90.81 10.32 88.44C10.69 85.98 11.53 83.55 12.37 81.19C12.84 79.88 13.53 78.64 14.24 77.44C14.87 76.39 15.56 75.36 16.37 74.44C17.97 72.65 19.61 70.84 21.45 69.30C22.90 68.09 24.56 67.07 26.24 66.19C27.81 65.37 29.51 64.76 31.20 64.20C32.33 63.83 33.52 63.60 34.70 63.42C70.27 57.92 105.84 52.38 141.45 47.15C146.09 46.47 150.78 45.91 155.45 45.69Z'
// hard offset shadow, measured from the guide
const SHADOW = { dx: 1, dy: 10 }
// wordmark bar and text, measured from the primary logo (slide 1-1)
const BAR = { x: 6.12, y: 176.85, w: 427.13, h: 44.06 }
const TEXT = { x: 23.25, y: 208.67, length: 400.2, size: 36.1 }
// clearance space: x = one fifth of the primary logo's height (slide 1-4)
const X = 44.18

const viewBox = computed(() => {
  const width = 431.8
  const height = props.wordmark ? 220.91 : 187
  const pad = props.clearance ? X : 0
  return `${-pad} ${-pad} ${width + pad * 2} ${height + pad * 2}`
})

const titleId = useId()
</script>

<template>
  <svg
    class="g-logo"
    :class="[`g-logo--${size}`, `g-logo--${variant}`, `g-logo--on-${on}`]"
    :viewBox="viewBox"
    xmlns="http://www.w3.org/2000/svg"
    :role="decorative ? undefined : 'img'"
    :aria-hidden="decorative || undefined"
    :aria-labelledby="decorative ? undefined : titleId"
    focusable="false"
  >
    <title
      v-if="!decorative"
      :id="titleId"
    >{{ title }}</title>
    <path
      class="g-logo__shadow"
      :d="FRAME_PATH"
      :transform="`translate(${SHADOW.dx} ${SHADOW.dy})`"
      fill-rule="evenodd"
    />
    <path
      class="g-logo__frame"
      :d="FRAME_PATH"
      fill-rule="evenodd"
    />
    <g v-if="wordmark">
      <rect
        class="g-logo__bar"
        :x="BAR.x"
        :y="BAR.y"
        :width="BAR.w"
        :height="BAR.h"
      />
      <text
        class="g-logo__text"
        :x="TEXT.x"
        :y="TEXT.y"
        :textLength="TEXT.length"
        lengthAdjust="spacing"
        :font-size="TEXT.size"
        aria-hidden="true"
      >GeeksHacking</text>
    </g>
  </svg>
</template>

<style scoped>
.g-logo {
  --g-logo-frame: var(--gh-logo-frame);
  --g-logo-shadow: var(--gh-logo-shadow);
  --g-logo-bar: var(--gh-logo-bar);
  --g-logo-ink: var(--gh-logo-ink);
  --g-logo-mono: var(--gh-logo-mono);
  --g-logo-knockout: var(--gh-bg);

  display: inline-block;
  flex: none;
  width: auto;
  height: var(--g-logo-height);
  overflow: visible;
  vertical-align: middle;
}
.g-logo--sm { --g-logo-height: 28px; }
.g-logo--md { --g-logo-height: 40px; }
.g-logo--lg { --g-logo-height: 64px; }
.g-logo--xl { --g-logo-height: 120px; }

/* forced backgrounds (from the guide's colour variants, slides 1-1 and 1-3) */
.g-logo--on-light {
  --g-logo-frame: #fff200; --g-logo-shadow: #403f41; --g-logo-bar: #403f41; --g-logo-ink: #f5f5f5;
  --g-logo-mono: #1f1f1f; --g-logo-knockout: #f5f5f5;
}
.g-logo--on-dark {
  --g-logo-frame: #fff200; --g-logo-shadow: #403f41; --g-logo-bar: #403f41; --g-logo-ink: #f5f5f5;
  --g-logo-mono: #fff200; --g-logo-knockout: #1f1f1f;
}
.g-logo--on-yellow {
  --g-logo-frame: #656565; --g-logo-shadow: #403f41; --g-logo-bar: #403f41; --g-logo-ink: #f5f5f5;
  --g-logo-mono: #1f1f1f; --g-logo-knockout: #fff200;
}

.g-logo__shadow { fill: var(--g-logo-shadow); }
.g-logo__frame { fill: var(--g-logo-frame); }
.g-logo__bar { fill: var(--g-logo-bar); }
.g-logo__text {
  fill: var(--g-logo-ink);
  font-family: var(--font-display);
  font-weight: 500;
}

/* mono: one colour; frame and shadow merge into one thicker frame, text knocked out of the bar */
.g-logo--mono .g-logo__shadow,
.g-logo--mono .g-logo__frame,
.g-logo--mono .g-logo__bar { fill: var(--g-logo-mono); }
.g-logo--mono .g-logo__text { fill: var(--g-logo-knockout); }
</style>
