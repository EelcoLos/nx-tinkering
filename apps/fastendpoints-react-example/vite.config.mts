/// <reference types='vitest' />
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react';
import { nxCopyAssetsPlugin } from '@nx/vite/plugins/nx-copy-assets.plugin';
import { nxViteTsPaths } from '@nx/vite/plugins/nx-tsconfig-paths.plugin';
import { defineConfig } from 'vite';

const appRoot = dirname(fileURLToPath(import.meta.url));

export default defineConfig(() => ({
  root: appRoot,
  cacheDir: '../../node_modules/.vite/apps/fastendpoints-react-example',
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
  plugins: [react(), nxViteTsPaths(), nxCopyAssetsPlugin(['*.md'])],
  // Uncomment this if you are using workers.
  // worker: {
  //   plugins: () => [ nxViteTsPaths() ],
  // },
  build: {
    outDir: resolve(appRoot, '../../dist/apps/fastendpoints-react-example'),
    emptyOutDir: true,
    reportCompressedSize: true,
    commonjsOptions: {
      transformMixedEsModules: true,
    },
  },
}));
