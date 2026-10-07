import { join } from 'node:path';
import type { CreateDependenciesContext } from '@nx/devkit';
import {
  createDependencies,
  owningProject,
  readBuilds,
} from '../../../tools/nx-plugins/docker-compose';

const workspaceRoot = join(__dirname, '../../..');
const demo = 'apps/a2a-docker-demo';

describe('docker-compose plugin', () => {
  it('resolves build sections to workspace-relative paths', () => {
    expect(
      readBuilds(workspaceRoot, `${demo}/docker-compose.local.yml`),
    ).toContainEqual({
      context: demo,
      dockerfile: `${demo}/identity/Dockerfile`,
    });
    expect(readBuilds(workspaceRoot, `${demo}/docker-compose.yml`)).toEqual([]);
  });

  it('picks the project with the longest matching root', () => {
    const projects = {
      demo: { root: demo },
      identity: { root: `${demo}/identity` },
      'identity-other': { root: `${demo}/identity-other` },
    };
    expect(owningProject(`${demo}/identity/Dockerfile`, projects)).toBe(
      'identity',
    );
    expect(owningProject(`${demo}/website/Dockerfile`, projects)).toBe('demo');
    expect(owningProject('apps/other/Dockerfile', projects)).toBeUndefined();
  });

  it('adds edges from the compose project to the service projects', async () => {
    const context = {
      workspaceRoot,
      projects: {
        'a2a-docker-demo': { root: demo },
        identity: { root: `${demo}/identity` },
        router: { root: `${demo}/router` },
      },
    } as unknown as CreateDependenciesContext;
    const deps = await createDependencies(undefined, context);
    expect(deps.map((d) => d.target)).toEqual(['identity', 'router']);
    expect(deps[0]).toMatchObject({
      source: 'a2a-docker-demo',
      sourceFile: `${demo}/docker-compose.local.yml`,
      type: 'static',
    });
  });
});
