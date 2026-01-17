// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  modules: [
    '@nuxt/eslint',
    '@nuxt/hints',
    '@nuxt/image',
    '@nuxt/scripts',
    '@nuxt/test-utils',
    '@nuxt/ui',
    '@nuxt/fonts',
  ],
  ssr: false,
  devtools: { enabled: true },

  // allows auto-import for constants
  imports: {
    dirs: ['composables/constants'],
  },
  devtools: { enabled: true },

  css: ['~/assets/css/main.css'],

  runtimeConfig: {
    public: {
      api: process.env['services__api__https__0'] || process.env.SERVICES__API_HTTPS_0 || 'https://hackomania-api.geekshacking.com',
    },
  },
  compatibilityDate: '2025-07-15',

  nitro: {
    cloudflare: {
      wrangler: {
        keep_vars: true,
        observability: {
          logs: {
            enabled: true,
            head_sampling_rate: 1,
            invocation_logs: true,
          },
        },
      },
    },
  },

  eslint: {
    config: {
      stylistic: true,
    },
  },
})
