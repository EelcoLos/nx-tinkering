import {
  createNodesFromFiles,
  DependencyType,
  joinPathFragments,
  type CreateDependencies,
  type CreateNodesV2,
  type RawProjectGraphDependency,
} from '@nx/devkit';
import { existsSync, readFileSync } from 'node:fs';
import { basename, dirname, join } from 'node:path';

const configGlob = '**/openapi-ts.config.ts';

// Workspace-relative `input`/`output` paths of a hey-api config.
// ponytail: regex takes the first string literal after `input:`/`output:`,
// resolved against the config dir (i.e. `resolve(appRoot, '<literal>')`).
// Variables, URLs or multiple inputs/outputs are not understood.
export function readOpenApiTsPaths(configFile: string, source: string) {
  const dir = dirname(configFile);
  const literalAfter = (key: string) =>
    source.match(
      new RegExp(`\\b${key}\\s*:[^'"\`]*['"\`]([^'"\`]+)['"\`]`),
    )?.[1];
  const input = literalAfter('input');
  const output = literalAfter('output');
  return {
    input: input && joinPathFragments(dir, input),
    output: output && joinPathFragments(dir, output),
  };
}

export const createNodesV2: CreateNodesV2 = [
  configGlob,
  (configFiles, _options, context) =>
    createNodesFromFiles(
      (configFile, _opts, ctx) => {
        const root = dirname(configFile);
        if (
          !existsSync(join(ctx.workspaceRoot, root, 'project.json')) &&
          !existsSync(join(ctx.workspaceRoot, root, 'package.json'))
        ) {
          return {};
        }
        const { input, output } = readOpenApiTsPaths(
          configFile,
          readFileSync(join(ctx.workspaceRoot, configFile), 'utf-8'),
        );
        return {
          projects: {
            [root]: {
              targets: {
                codegen: {
                  command: `npx openapi-ts -f ${basename(configFile)}`,
                  options: { cwd: root },
                  cache: true,
                  // The spec owner is a project dependency (see createDependencies),
                  // so ^build builds the API, which exports the spec.
                  dependsOn: ['^build'],
                  inputs: [
                    `{workspaceRoot}/${configFile}`,
                    ...(input ? [`{workspaceRoot}/${input}`] : []),
                    { externalDependencies: ['@hey-api/openapi-ts'] },
                  ],
                  outputs: output ? [`{workspaceRoot}/${output}`] : [],
                },
                // '...' keeps the dependsOn other plugins already inferred.
                build: { dependsOn: ['...', 'codegen'] },
                typecheck: { dependsOn: ['...', 'codegen'] },
                test: { dependsOn: ['...', 'codegen'] },
              },
            },
          },
        };
      },
      configFiles,
      _options,
      context,
    ),
];

export const createDependencies: CreateDependencies = (_options, context) => {
  const deps: RawProjectGraphDependency[] = [];
  for (const [source, files] of Object.entries(
    context.fileMap.projectFileMap,
  )) {
    for (const { file } of files) {
      if (basename(file) !== basename(configGlob)) continue;
      const { input } = readOpenApiTsPaths(
        file,
        readFileSync(join(context.workspaceRoot, file), 'utf-8'),
      );
      if (!input) continue;
      const target = Object.entries(context.projects)
        .filter(([, p]) => input.startsWith(`${p.root}/`))
        .sort(([, a], [, b]) => b.root.length - a.root.length)[0]?.[0];
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
  return deps;
};
