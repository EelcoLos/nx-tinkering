/// <reference types='vitest/config' />
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import babel from '@rolldown/plugin-babel';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig(() => ({
  root: appRoot,
  cacheDir: '../../node_modules/.vite/apps/fastendpoints-react-example',
  resolve: {
    tsconfigPaths: true,
  },
  server: {
    port: 4300,
    host: 'localhost',
    proxy: {
      '/api': {
        changeOrigin: true,
        secure: false,
        target: 'https://localhost:5002',
      },
    },
  },
  preview: {
    port: 4300,
    host: 'localhost',
  },
  // React Compiler (babel-plugin-react-compiler) via the plugin-react preset.
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  build: {
    outDir: resolve(appRoot, '../../dist/apps/fastendpoints-react-example'),
    emptyOutDir: true,
    reportCompressedSize: true,
  },
  test: {
    name: 'fastendpoints-react-example',
    watch: false,
    globals: true,
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    reporters: ['default'],
    coverage: {
      reportsDirectory: '../../coverage/apps/fastendpoints-react-example',
      provider: 'v8' as const,
    },
  },
}));
