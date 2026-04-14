import { defineConfig } from 'orval';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  'fastendpoints-react-example': {
    input: resolve(appRoot, '../fastendpoints-react-api/wwwroot/api/specification.json'),
    output: {
      target: resolve(appRoot, './src/generated/orval/index.ts'),
      client: 'react-query',
      mode: 'single',
    },
  },
});
