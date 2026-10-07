import { existsSync, readFileSync } from 'node:fs';
import { join, posix } from 'node:path';
import {
  createNodesFromFiles,
  DependencyType,
  type CreateDependencies,
  type CreateNodesV2,
  type ProjectConfiguration,
  type RawProjectGraphDependency,
} from '@nx/devkit';
// `yaml` is a direct dependency of `nx`, so it is always installed.
import { parse } from 'yaml';

const COMPOSE_FILE = 'docker-compose.yml';
// docker-compose.yml is the Swarm stack (prebuilt images only); the repo's scripts
// and docs run the local stack from this file, which has the `build:` sections.
const LOCAL_COMPOSE_FILE = 'docker-compose.local.yml';

export interface ComposeBuild {
  /** Workspace-relative build context directory. */
  context: string;
  /** Workspace-relative Dockerfile path. */
  dockerfile: string;
}

/** Compose files (workspace-relative) of the stack in `root`; empty if none. */
export function composeFiles(workspaceRoot: string, root: string): string[] {
  if (!existsSync(join(workspaceRoot, root, COMPOSE_FILE))) return [];
  return [COMPOSE_FILE, LOCAL_COMPOSE_FILE]
    .map((f) => posix.join(root, f))
    .filter((f) => existsSync(join(workspaceRoot, f)));
}

/** The `build:` section of every service, resolved to workspace-relative paths. */
export function readBuilds(
  workspaceRoot: string,
  composeFile: string,
): ComposeBuild[] {
  const doc = parse(readFileSync(join(workspaceRoot, composeFile), 'utf-8'));
  const services: Record<string, { build?: string | Record<string, string> }> =
    doc?.services ?? {};
  return Object.values(services).flatMap(({ build }) => {
    if (!build) return [];
    const spec = typeof build === 'string' ? { context: build } : build;
    const context = posix.join(posix.dirname(composeFile), spec.context ?? '.');
    return [
      {
        context,
        dockerfile: posix.join(context, spec.dockerfile ?? 'Dockerfile'),
      },
    ];
  });
}

/** Name of the project with the longest root containing `file`. */
export function owningProject(
  file: string,
  projects: Record<string, Pick<ProjectConfiguration, 'root'>>,
): string | undefined {
  let best: string | undefined;
  let bestLength = -1;
  for (const [name, { root }] of Object.entries(projects)) {
    const inside =
      root === '.' ||
      root === '' ||
      file === root ||
      file.startsWith(`${root}/`);
    if (inside && root.length > bestLength) {
      best = name;
      bestLength = root.length;
    }
  }
  return best;
}

export const createNodesV2: CreateNodesV2 = [
  `**/${COMPOSE_FILE}`,
  (files, _options, context) =>
    createNodesFromFiles(
      (file, _opts, ctx) => {
        const root = posix.dirname(file);
        const paths = composeFiles(ctx.workspaceRoot, root);
        const builds = paths.flatMap((f) => readBuilds(ctx.workspaceRoot, f));
        const compose = `docker compose -f ${posix.basename(paths[paths.length - 1])}`;
        const targets: ProjectConfiguration['targets'] = {
          'compose-up': {
            // --wait implies detached mode and blocks until services are healthy.
            command: `${compose} up --build --wait`,
            options: { cwd: root },
            cache: false,
            // Images are built from source inside Docker (multi-stage
            // `dotnet publish`), so no dependsOn on the projects' `build`.
            inputs: [
              ...new Set([
                ...paths,
                ...builds.map((b) => b.dockerfile),
                ...builds.map((b) => `${b.context}/**/*`),
              ]),
            ].map((f) => `{workspaceRoot}/${f}`),
          },
          'compose-down': {
            command: `${compose} down`,
            options: { cwd: root },
            cache: false,
          },
        };
        if (existsSync(join(ctx.workspaceRoot, root, 'test-e2e.sh'))) {
          targets.e2e = {
            command: 'bash test-e2e.sh',
            options: { cwd: root },
            dependsOn: ['compose-up'],
            cache: false,
          };
        }
        return {
          projects: {
            [root]: {
              name: posix.basename(root),
              tags: ['docker-compose'],
              targets,
            },
          },
        };
      },
      files,
      _options,
      context,
    ),
];

export const createDependencies: CreateDependencies = (_options, context) => {
  const dependencies: RawProjectGraphDependency[] = [];
  for (const [source, { root }] of Object.entries(context.projects)) {
    for (const sourceFile of composeFiles(context.workspaceRoot, root)) {
      for (const build of readBuilds(context.workspaceRoot, sourceFile)) {
        // Prefer the Dockerfile's project: a shared context (e.g. `.`) usually
        // resolves back to the compose project itself.
        const target = [build.dockerfile, build.context]
          .map((path) => owningProject(path, context.projects))
          .find((name) => name && name !== source);
        if (target) {
          dependencies.push({
            source,
            target,
            sourceFile,
            type: DependencyType.static,
          });
        }
      }
    }
  }
  return dependencies;
};
