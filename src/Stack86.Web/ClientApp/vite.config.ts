/// <reference types="vitest" />
import { defineConfig, type Plugin } from 'vite';
import angular from '@analogjs/vite-plugin-angular';
import mkcert from 'vite-plugin-mkcert';
import { resolve } from 'node:path';
import { readFileSync } from 'node:fs';
import fg from 'fast-glob';

const isWatch = process.argv.includes('--watch');

/**
 * Intercepts `?raw` imports for `.ts` files under `samples/ts/` so Vite
 * returns the raw source text instead of compiling it as TypeScript.
 * Resolves to a virtual `.js` path so the Angular plugin skips TS compilation.
 */
function rawTsSamples(): Plugin {
  return {
    name: 'raw-ts-samples',
    enforce: 'pre',
    resolveId(source, importer) {
      if (source.includes('samples/ts/') && source.endsWith('.ts?raw')) {
        const dir = importer ? resolve(importer, '..') : import.meta.dirname;
        const filePath = resolve(dir, source.replace('?raw', ''));
        return filePath.replace(/\.ts$/, '.__raw__.js');
      }
    },
    load(id) {
      if (id.endsWith('.__raw__.js') && id.includes('samples')) {
        const filePath = id.replace(/\.__raw__\.js$/, '.ts');
        this.addWatchFile(filePath);
        const content = readFileSync(filePath, 'utf-8');
        return `export default ${JSON.stringify(content)};`;
      }
    },
  };
}

function watchComponentAssets(): Plugin {
  return {
    name: 'watch-component-assets',
    apply: 'build',
    buildStart() {
      if (!isWatch) return;
      const patterns = ['src/**/*.component.scss', 'src/**/*.component.html', 'src/styles.scss'];
      const files = fg.sync(patterns, { cwd: import.meta.dirname });
      for (const file of files) {
        this.addWatchFile(resolve(import.meta.dirname, file));
      }
    },
  };
}

export default defineConfig(({ mode }) => {
  const isTest = mode === 'test' || process.env['VITEST'] !== undefined;
  const isDev = mode === 'development';

  return {
    root: import.meta.dirname,
    publicDir: 'public',

    build: {
      outDir: '../wwwroot',
      emptyOutDir: true,
      sourcemap:
        process.env['VITE_SOURCEMAPS'] === 'true' ? 'inline' : isDev,
      minify: !isDev,
      target: ['es2022'],
      reportCompressedSize: !isDev,
      // The Monaco editor ships as its own large vendor chunk, lazily loaded; it
      // is expected to exceed the default 500 kB advisory.
      chunkSizeWarningLimit: 4000,
      // Plugin-timing output is a build-performance diagnostic, not an issue.
      // sourcemapBroken fires in dev-mode builds because the Angular optimizer
      // does not forward a sourcemap for its transform; it is a known Analog
      // limitation, not a fault in this project.
      rolldownOptions: {
        checks: { pluginTimings: false, sourcemapBroken: false },
      },
    },

    plugins: [
      rawTsSamples(),
      watchComponentAssets(),
      angular({
        inlineStylesExtension: 'scss',
        jit: isTest,
      }),
      !isTest && mkcert(),
    ],

    resolve: {
      mainFields: ['module'],
      alias: [
        { find: /^@core\/(.*)$/, replacement: resolve(import.meta.dirname, 'src/app/core/$1') },
        { find: /^@features\/(.*)$/, replacement: resolve(import.meta.dirname, 'src/app/features/$1') },
        { find: /^@shared\/(.*)$/, replacement: resolve(import.meta.dirname, 'src/app/shared/$1') },
        { find: /^@state\/(.*)$/, replacement: resolve(import.meta.dirname, 'src/app/state/$1') },
      ],
    },

    server: {
      port: 1234,
      proxy: {
        '/api/Compiler/compileStream': {
          target: 'http://localhost:1998',
          changeOrigin: true,
          configure: (proxy) => {
            proxy.on('proxyRes', (proxyRes) => {
              // Disable buffering so NDJSON lines stream through in real-time
              proxyRes.headers['cache-control'] = 'no-cache';
              proxyRes.headers['x-accel-buffering'] = 'no';
            });
          },
        },
        '/api': {
          target: 'http://localhost:1998',
          changeOrigin: true,
        },
      },
    },

    css: {
      preprocessorOptions: {
        scss: {
          silenceDeprecations: ['import', 'global-builtin'],
        },
      },
    },

    test: {
      globals: true,
      setupFiles: ['src/test-setup.ts'],
      environment: 'jsdom',
      include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
      reporters: ['default'],
      css: false,
    },
  };
});
