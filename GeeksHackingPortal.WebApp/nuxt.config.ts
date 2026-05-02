/* eslint-disable node/prefer-global/process */
// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  modules: [
    '@nuxt/hints',
    '@nuxt/image',
    '@nuxt/scripts',
    '@nuxt/test-utils',
    '@nuxt/ui',
  ],

  ssr: false,

  // allows auto-import for constants
  imports: {
    dirs: ['composables/constants'],
  },
  devtools: { enabled: true },

  // https://github.com/nuxt/nuxt/issues/34957
  experimental: {
    viteEnvironmentApi: true,
  },

  $production: {
    scripts: {
      registry: {
        clarity: {
          id: 'v9hg1hk686',
        },
      },
    },
  },

  css: ['~/assets/css/main.css'],

  colorMode: {
    preference: 'light',
    fallback: 'light',
  },

  runtimeConfig: {
    public: {
      api: process.env.services__api__http__0 || process.env.SERVICES__API_HTTP_0 || 'https://hackomania-api.geekshacking.com',
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
