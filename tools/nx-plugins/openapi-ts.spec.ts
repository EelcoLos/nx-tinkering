// Run: node --test tools/nx-plugins/openapi-ts.spec.ts
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { test } from 'node:test';
import { readOpenApiTsPaths } from './openapi-ts.ts';

test('resolves input/output of the real hey-api config', () => {
  const config = 'apps/fastendpoints-react-example/openapi-ts.config.ts';
  const source = readFileSync(
    join(import.meta.dirname, '../..', config),
    'utf-8',
  );
  assert.deepEqual(readOpenApiTsPaths(config, source), {
    input: 'apps/fastendpoints-react-api/wwwroot/api/specification.json',
    output: 'apps/fastendpoints-react-example/src/generated/hey-api',
  });
});
