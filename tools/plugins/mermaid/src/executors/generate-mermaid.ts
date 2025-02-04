import { writeFileSync } from 'fs';
import { ExecutorContext, PromiseExecutor } from '@nx/devkit';
import { GenerateMermaidExecutorSchema } from './schema';

const runExecutor: PromiseExecutor<GenerateMermaidExecutorSchema> = async (
  options,
  context: ExecutorContext,
) => {
  const diagram = buildMermaidDiagram(context.projectGraph, !!options.affected);
  const markdownContent = `# Project Graph\n\n\`\`\`mermaid\n${diagram}\n\`\`\``;

  if (options.outputPath) {
    writeFileSync(options.outputPath, markdownContent);
  } else {
    console.log(markdownContent);
  }

  return {
    success: true,
  };
};

function buildMermaidDiagram(projectGraph: any, affected: boolean): string {
  const nodes = Object.keys(projectGraph.nodes)
    .map((node) => {
      const style =
        affected && projectGraph.nodes[node].affected ? ' style affected' : '';
      return `    ${node}${style}`;
    })
    .join('\n');
  return `graph LR\n${nodes}`;
}

export default runExecutor;
