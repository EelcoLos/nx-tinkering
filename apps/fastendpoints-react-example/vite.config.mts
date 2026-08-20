/// <reference types='vitest' />
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
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
  plugins: [react()],
  build: {
    outDir: resolve(appRoot, '../../dist/apps/fastendpoints-react-example'),
    emptyOutDir: true,
    reportCompressedSize: true,
    commonjsOptions: {
      transformMixedEsModules: true,
    },
  },
}));
