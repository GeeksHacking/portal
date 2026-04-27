import { defineConfig } from '@kubb/core'
import { pluginFaker } from '@kubb/plugin-faker'
import { pluginMsw } from '@kubb/plugin-msw'
import { pluginOas } from '@kubb/plugin-oas'
import { pluginTs } from '@kubb/plugin-ts'
import { pluginVueQuery } from '@kubb/plugin-vue-query'
import { pluginZod } from '@kubb/plugin-zod'

export default defineConfig({
  root: '.',
  input: {
    path: `${process.env.services__api__http__0}/openapi/v1.json`,
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
    pluginVueQuery({
      client: {
        importPath: '../../client/fetch',
      },
    }),
    pluginMsw(),
    pluginFaker(),
    pluginZod(),
  ],
})
