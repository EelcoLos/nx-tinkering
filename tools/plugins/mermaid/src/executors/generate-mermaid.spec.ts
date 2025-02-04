import { ExecutorContext } from '@nx/devkit';
import { readFileSync, unlinkSync } from 'fs';

import { GenerateMermaidExecutorSchema } from './schema';
import executor from './generate-mermaid';

const options: GenerateMermaidExecutorSchema = { outputPath: 'diagram.md' };
const context: ExecutorContext = {
  root: '',
  cwd: process.cwd(),
  isVerbose: false,
  projectGraph: {
    nodes: {
      projectA: {
        type: 'lib',
        name: 'projectA',
        data: { root: 'libs/projectA' },
      },
      projectB: {
        type: 'lib',
        name: 'projectB',
        data: { root: 'libs/projectB' },
      },
      projectC: {
        type: 'lib',
        name: 'projectC',
        data: { root: 'libs/projectC' },
      },
      projectD: {
        type: 'lib',
        name: 'projectD',
        data: { root: 'libs/projectD' },
      },
    },
    dependencies: {},
  },
  projectsConfigurations: {
    projects: {},
    version: 2,
  },
  nxJsonConfiguration: {},
};

describe('GenerateMermaid Executor', () => {
  it('can run and produce a mermaid diagram', async () => {
    const output = await executor(options, context);
    expect(output.success).toBe(true);

    const content = readFileSync(options.outputPath, 'utf-8');
    expect(content).toContain('```mermaid');
    expect(content).toContain('graph LR');
    expect(content).toContain('projectA');
    expect(content).toContain('projectB');
    expect(content).toContain('projectC');
    expect(content).toContain('projectD');

    // Clean up
    unlinkSync(options.outputPath);
  });

  it('can run and produce a mermaid diagram with affected nodes', async () => {
    const affectedOptions: GenerateMermaidExecutorSchema = {
      outputPath: 'affected-diagram.md',
      affected: true,
    };
    const output = await executor(affectedOptions, context);
    expect(output.success).toBe(true);

    const content = readFileSync(affectedOptions.outputPath, 'utf-8');
    expect(content).toContain('```mermaid');
    expect(content).toContain('graph LR');
    expect(content).toContain('projectA');
    expect(content).toContain('projectB style affected');
    expect(content).toContain('projectC');
    expect(content).toContain('projectD');

    // Clean up
    unlinkSync(affectedOptions.outputPath);
  });
});
