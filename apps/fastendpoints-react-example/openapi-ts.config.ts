import { defineConfig } from '@hey-api/openapi-ts';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  input: resolve(
    appRoot,
    '../fastendpoints-react-api/wwwroot/api/specification.json',
  ),
  output: resolve(appRoot, './src/generated/hey-api'),
  plugins: [
    {
      name: '@tanstack/react-query',
      includeInEntry: true,
      queryOptions: true,
      queryKeys: true,
      mutationOptions: true,
    },
  ],
});
