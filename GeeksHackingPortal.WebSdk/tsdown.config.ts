import { defineConfig } from 'tsdown'

export default defineConfig({
  name: '@geekshacking/portal-sdk',
  entry: [
    './src/index.ts',
    './src/hooks.ts',
    './src/zod.ts',
    './src/testing.ts',
    './src/testing/handlers.ts',
    './src/testing/mocks.ts',
    './src/client/index.ts',
    './src/client/fetch.ts',
  ],
  root: './src',
  platform: 'neutral',
  target: 'es2022',
  format: ['esm'],
  dts: true,
  clean: true,
  unbundle: true,
  treeshake: {
    moduleSideEffects: false,
  },
  checks: {
    pluginTimings: false,
  },
  report: false,
})
