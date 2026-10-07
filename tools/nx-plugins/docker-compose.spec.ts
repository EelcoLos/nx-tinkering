// Run: node --test tools/nx-plugins/docker-compose.spec.ts
import assert from 'node:assert/strict';
import { join } from 'node:path';
import { test } from 'node:test';
import type { CreateDependenciesContext } from '@nx/devkit';
import {
  createDependencies,
  owningProject,
  readBuilds,
} from './docker-compose.ts';

const workspaceRoot = join(import.meta.dirname, '../..');
const demo = 'apps/a2a-docker-demo';

test('resolves build sections to workspace-relative paths', () => {
  const builds = readBuilds(workspaceRoot, `${demo}/docker-compose.local.yml`);
  assert.ok(
    builds.some(
      (b) =>
        b.context === demo && b.dockerfile === `${demo}/identity/Dockerfile`,
    ),
  );
  assert.deepEqual(readBuilds(workspaceRoot, `${demo}/docker-compose.yml`), []);
});

test('picks the project with the longest matching root', () => {
  const projects = {
    demo: { root: demo },
    identity: { root: `${demo}/identity` },
    'identity-other': { root: `${demo}/identity-other` },
  };
  assert.equal(
    owningProject(`${demo}/identity/Dockerfile`, projects),
    'identity',
  );
  assert.equal(owningProject(`${demo}/website/Dockerfile`, projects), 'demo');
  assert.equal(owningProject('apps/other/Dockerfile', projects), undefined);
});

test('adds edges from the compose project to the service projects', async () => {
  const context = {
    workspaceRoot,
    projects: {
      'a2a-docker-demo': { root: demo },
      identity: { root: `${demo}/identity` },
      router: { root: `${demo}/router` },
    },
  } as unknown as CreateDependenciesContext;
  const deps = await createDependencies(undefined, context);
  assert.deepEqual(
    deps.map((d) => d.target),
    ['identity', 'router'],
  );
  assert.equal(deps[0].source, 'a2a-docker-demo');
  assert.equal(deps[0].sourceFile, `${demo}/docker-compose.local.yml`);
  assert.equal(deps[0].type, 'static');
});
