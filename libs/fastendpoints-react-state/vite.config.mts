/// <reference types='vitest/config' />
import { defineConfig } from 'vite';

export default defineConfig(() => ({
  root: import.meta.dirname,
  cacheDir: '../../node_modules/.vite/libs/fastendpoints-react-state',
  resolve: {
    tsconfigPaths: true,
  },
  test: {
    name: 'fastendpoints-react-state',
    watch: false,
    globals: true,
    environment: 'node',
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    reporters: ['default'],
    coverage: {
      reportsDirectory: '../../coverage/libs/fastendpoints-react-state',
      provider: 'v8' as const,
    },
  },
}));
