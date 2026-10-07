// Run: node --test tools/nx-plugins/dotnet-file-apps.spec.ts
import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { after, test } from 'node:test';
import { createDependencies, isFileBasedApp } from './dotnet-file-apps.ts';

const root = mkdtempSync(join(tmpdir(), 'dotnet-file-apps-'));
const write = (file: string, content = '') => {
  mkdirSync(dirname(join(root, file)), { recursive: true });
  writeFileSync(join(root, file), content);
};

write('apps/app1/app.cs', '#:sdk Microsoft.NET.Sdk.Web\nvar x = 1;\n');
write('apps/app1/Helper.cs', 'class Helper {}\n');
write('apps/two/b.cs', '#:package Foo@1.0.0\n');
write('apps/two/a.cs', '#!/usr/bin/env dotnet\n');
write('apps/plain/Program.cs', 'Console.WriteLine();\n');
write('apps/proj/proj.csproj', '<Project />');
write('apps/proj/nested/tool.cs', '#:sdk Microsoft.NET.Sdk\n');

test('detects a file-based app by its #: directive', () => {
  assert.equal(isFileBasedApp(root, 'apps/app1/app.cs'), true);
  assert.equal(isFileBasedApp(root, 'apps/app1/Helper.cs'), false);
});

test('picks the alphabetically first entry point per directory', () => {
  assert.equal(isFileBasedApp(root, 'apps/two/a.cs'), true);
  assert.equal(isFileBasedApp(root, 'apps/two/b.cs'), false);
});

test('ignores files without directives', () => {
  assert.equal(isFileBasedApp(root, 'apps/plain/Program.cs'), false);
});

test('ignores files under a csproj', () => {
  assert.equal(isFileBasedApp(root, 'apps/proj/nested/tool.cs'), false);
});

test('#:project directive adds an edge to the owning project', async () => {
  write(
    'apps/app1/app.cs',
    '#:sdk Microsoft.NET.Sdk\n#:project ../../libs/lib/src\n',
  );
  const context = {
    workspaceRoot: root,
    projects: {
      app1: { root: 'apps/app1', tags: ['file-based'] },
      lib: { root: 'libs/lib' },
    },
    fileMap: {
      projectFileMap: { app1: [{ file: 'apps/app1/app.cs', hash: '' }] },
      nonProjectFiles: [],
    },
  } as unknown as Parameters<typeof createDependencies>[1];
  assert.deepEqual(await createDependencies(undefined, context), [
    {
      source: 'app1',
      target: 'lib',
      sourceFile: 'apps/app1/app.cs',
      type: 'static',
    },
  ]);
});

after(() => rmSync(root, { recursive: true, force: true }));
