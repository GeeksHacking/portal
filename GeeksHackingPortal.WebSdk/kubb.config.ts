import { defineConfig } from '@kubb/core'
import { pluginClient } from '@kubb/plugin-client'
import { pluginFaker } from '@kubb/plugin-faker'
import { pluginMsw } from '@kubb/plugin-msw'
import { pluginOas } from '@kubb/plugin-oas'
import { pluginTs } from '@kubb/plugin-ts'
import { pluginVueQuery } from '@kubb/plugin-vue-query'
import { pluginZod } from '@kubb/plugin-zod'

export default defineConfig({
  root: '.',
  input: {
    path: 'http://localhost:5227/openapi/v1.json',
  },
  output: {
    path: './src/gen',
    clean: true,
    format: false,
    barrelType: false,
  },
  plugins: [
    pluginOas(),
    pluginTs(),
    pluginClient(),
    pluginVueQuery(),
    pluginMsw(),
    pluginFaker(),
    pluginZod(),
  ],
})
