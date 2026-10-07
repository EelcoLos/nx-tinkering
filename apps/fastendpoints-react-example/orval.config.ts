import { defineConfig } from 'orval';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  'fastendpoints-react-example': {
    input: resolve(
      appRoot,
      '../fastendpoints-react-api/wwwroot/api/v1.json',
    ),
    output: {
      target: resolve(appRoot, './src/generated/orval/index.ts'),
      client: 'react-query',
      mode: 'single',
      override: {
        // Adds the bearer token and throws on non-2xx (see src/app/api-clients.ts).
        mutator: {
          path: resolve(appRoot, './src/app/api-clients.ts'),
          name: 'orvalFetch',
        },
        fetch: {
          includeHttpResponseReturnType: false,
        },
      },
    },
  },
});
