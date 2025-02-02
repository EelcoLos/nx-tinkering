import { PromiseExecutor } from '@nx/devkit';
import { GenerateMermaidExecutorSchema } from './schema';

const runExecutor: PromiseExecutor<GenerateMermaidExecutorSchema> = async (
  options,
) => {
  console.log('Executor ran for GenerateMermaid', options);
  return {
    success: true,
  };
};

export default runExecutor;
