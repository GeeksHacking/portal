import { defineConfig } from 'kubb/config'
import { pluginClient } from '@kubb/plugin-client'
import { pluginFetch } from '@kubb/plugin-fetch'
import { pluginFaker } from '@kubb/plugin-faker'
import { pluginMsw } from '@kubb/plugin-msw'
import { pluginOas } from '@kubb/plugin-oas'
import { pluginTs } from '@kubb/plugin-ts'
import { pluginVueQuery } from '@kubb/plugin-vue-query'
import { pluginZod } from '@kubb/plugin-zod'

export default defineConfig({
  root: '.',
  input: process.env.OPENAPI_URL ?? 'https://hackomania-api.geekshacking.com/openapi/v1.json',
  output: {
    path: './src/gen',
    clean: true,
    format: false,
    barrelType: false,
    postGenerate: [
      // {
      //   name: 'annotate-mutation-return-types',
      //   command: 'node ./scripts/annotate-mutation-hook-return-types.mjs',
      // },
    ],
  },
  plugins: [
    pluginOas(),
    pluginTs(),
    pluginClient(),
    pluginFetch(),
    pluginVueQuery({
      mutation: {
        importPath: '../../useMutation.ts',
      },
      hooks: true
    }),
    pluginMsw(),
    pluginFaker(),
    pluginZod(),
  ],
})
