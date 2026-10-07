import {
  createNodesFromFiles,
  DependencyType,
  type CreateDependencies,
  type CreateNodesV2,
  type RawProjectGraphDependency,
} from '@nx/devkit';
import { readdirSync, readFileSync } from 'node:fs';
import { join, posix } from 'node:path';

// .NET 10 file-based apps (`dotnet run app.cs`) have no csproj, so @nx/dotnet doesn't see them.
// Anchored at apps/ so un-ignored copies (e.g. .claude/worktrees/*) don't yield duplicate projects.
const csGlob = 'apps/**/*.cs';

// ponytail: a .cs file is a file-based app when a line starts with a `#:` directive
// (or `#!` shebang). Apps using only top-level statements without directives are missed.
const isEntryPoint = (source: string) => /^#[:!]/m.test(source);

const hasCsproj = (workspaceRoot: string, dir: string) =>
  readdirSync(join(workspaceRoot, dir)).some((f) => f.endsWith('.csproj'));

// True when `file` is the file-based app of its directory: no csproj in its dir or any
// ancestor, and it is the alphabetically first entry-point .cs file in its directory
// (other entry points next to it are ignored, since one project per directory).
export function isFileBasedApp(workspaceRoot: string, file: string) {
  const dir = posix.dirname(file);
  for (let d = dir; ; d = posix.dirname(d)) {
    if (hasCsproj(workspaceRoot, d)) return false;
    if (d === '.') break;
  }
  const first = readdirSync(join(workspaceRoot, dir))
    .filter((f) => f.endsWith('.cs'))
    .sort()
    .find((f) =>
      isEntryPoint(readFileSync(join(workspaceRoot, dir, f), 'utf8')),
    );
  return first === posix.basename(file);
}

export const createNodesV2: CreateNodesV2 = [
  csGlob,
  (files, options, context) =>
    createNodesFromFiles(
      (file, _options, { workspaceRoot }) => {
        if (!isFileBasedApp(workspaceRoot, file)) return {};
        const root = posix.dirname(file);
        const fileName = posix.basename(file);
        return {
          projects: {
            [root]: {
              name: root.replace(/^apps\//, '').replaceAll('/', '-'),
              projectType: 'application',
              tags: ['dotnet', 'file-based'],
              targets: {
                build: {
                  executor: 'nx:run-commands',
                  options: { command: `dotnet build ${fileName}`, cwd: root },
                  cache: true,
                  inputs: [
                    `{projectRoot}/${fileName}`,
                    `{projectRoot}/${fileName.replace(/\.cs$/, '.run.json')}`,
                    '{workspaceRoot}/Directory.Build.props',
                    '{workspaceRoot}/Directory.Build.targets',
                    '{workspaceRoot}/Directory.Packages.props',
                    '{workspaceRoot}/global.json',
                  ],
                  // Only valid because the root Directory.Build.props redirects output to dist/;
                  // without it file-based builds go to %TEMP%/dotnet/runfile (outside the workspace).
                  outputs: [
                    '{workspaceRoot}/dist/{projectRoot}',
                    '{workspaceRoot}/dist/intermediates/{projectRoot}/obj',
                  ],
                },
                // cwd = app dir so the app's content root (relative config files) matches `dotnet run app.cs`.
                run: {
                  executor: 'nx:run-commands',
                  options: { command: `dotnet run ${fileName}`, cwd: root },
                  continuous: true,
                  cache: false,
                },
              },
            },
          },
        };
      },
      files,
      options,
      context,
    ),
];

// `#:project <path>` directives -> static edge to the project owning that path.
export const createDependencies: CreateDependencies = (_options, context) => {
  const projects = Object.entries(context.projects);
  const owner = (path: string) =>
    projects
      .filter(
        ([, p]) =>
          p.root === '.' || path === p.root || path.startsWith(`${p.root}/`),
      )
      .sort(([, a], [, b]) => b.root.length - a.root.length)[0]?.[0];

  const deps: RawProjectGraphDependency[] = [];
  for (const [source, files] of Object.entries(
    context.fileMap.projectFileMap,
  )) {
    if (!context.projects[source]?.tags?.includes('file-based')) continue;
    for (const { file } of files) {
      if (!file.endsWith('.cs')) continue;
      const content = readFileSync(join(context.workspaceRoot, file), 'utf8');
      for (const [, ref] of content.matchAll(/^#:project\s+(.+?)\s*$/gm)) {
        const path = posix.join(posix.dirname(file), ref.replaceAll('\\', '/'));
        const target = owner(path);
        if (target && target !== source) {
          deps.push({
            source,
            target,
            sourceFile: file,
            type: DependencyType.static,
          });
        }
      }
    }
  }
  return deps;
};
