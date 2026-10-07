import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import {
  createDependencies,
  isFileBasedApp,
} from '../../../tools/nx-plugins/dotnet-file-apps';

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

afterAll(() => rmSync(root, { recursive: true, force: true }));

describe('dotnet-file-apps plugin', () => {
  it('detects a file-based app by its #: directive', () => {
    expect(isFileBasedApp(root, 'apps/app1/app.cs')).toBe(true);
    expect(isFileBasedApp(root, 'apps/app1/Helper.cs')).toBe(false);
  });

  it('picks the alphabetically first entry point per directory', () => {
    expect(isFileBasedApp(root, 'apps/two/a.cs')).toBe(true);
    expect(isFileBasedApp(root, 'apps/two/b.cs')).toBe(false);
  });

  it('ignores files without directives', () => {
    expect(isFileBasedApp(root, 'apps/plain/Program.cs')).toBe(false);
  });

  it('ignores files under a csproj', () => {
    expect(isFileBasedApp(root, 'apps/proj/nested/tool.cs')).toBe(false);
  });

  it('#:project directive adds an edge to the owning project', async () => {
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
    expect(await createDependencies(undefined, context)).toEqual([
      {
        source: 'app1',
        target: 'lib',
        sourceFile: 'apps/app1/app.cs',
        type: 'static',
      },
    ]);
  });
});
