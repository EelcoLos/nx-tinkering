import { ExecutorContext } from '@nx/devkit';

import { GenerateMermaidExecutorSchema } from './schema';
import executor from './generate-mermaid';

const options: GenerateMermaidExecutorSchema = {};
const context: ExecutorContext = {
  root: '',
  cwd: process.cwd(),
  isVerbose: false,
  projectGraph: {
    nodes: {},
    dependencies: {},
  },
  projectsConfigurations: {
    projects: {},
    version: 2,
  },
  nxJsonConfiguration: {},
};

describe('GenerateMermaid Executor', () => {
  it('can run', async () => {
    const output = await executor(options, context);
    expect(output.success).toBe(true);
  });
});
