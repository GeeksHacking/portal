import { defineConfig } from 'tsdown'

export default defineConfig(({ watch }) => ({
  name: '@geekshacking/portal-sdk',
  entry: [
    './src/index.ts',
    './src/hooks.ts',
    './src/zod.ts',
    './src/testing.ts',
    './src/testing/handlers.ts',
    './src/testing/mocks.ts',
    './src/client/index.ts'
  ],
  root: './src',
  platform: 'neutral',
  target: 'es2022',
  format: ['esm'],
  dts: true,
  // Watch mode (`pnpm dev`, the Aspire `sdk` resource) must never delete dist:
  // the WebApp dev server resolves @geekshacking/portal-sdk/* from it, and Vite
  // keeps a failed import until restarted, which shows up as a blank page.
  // With clean off, rebuilds overwrite files in place. One-off builds still clean.
  clean: !watch,
  unbundle: true,
  treeshake: {
    moduleSideEffects: false,
  },
  checks: {
    pluginTimings: false,
  },
  report: false,
}))
