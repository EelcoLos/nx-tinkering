import { defineConfig } from 'orval';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  'angular-auth-example': {
    input: {
      target: resolve(appRoot, '../dotnet-fe-auth/wwwroot/api/v1.json'),
      override: {
        // FastEndpoints.OpenApi emits `"const": null` for some FluentValidation
        // rules, which would turn LoginRequest into a literal-null type.
        transformer: (spec) =>
          JSON.parse(
            JSON.stringify(spec, (key, value) =>
              key === 'const' && value === null ? undefined : value,
            ),
          ),
      },
    },
    output: {
      target: resolve(appRoot, './src/api-integration/api.ts'),
      client: 'angular',
      mode: 'single',
    },
  },
});
