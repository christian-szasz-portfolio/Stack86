import { defineConfig, devices } from '@playwright/test';

const PORT = 1234;
const BASE_URL = `https://localhost:${PORT}`;

export default defineConfig({
  testDir: './tests',
  testMatch: /.*\.spec\.ts$/,
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 2 : undefined,
  timeout: 30_000,
  expect: { timeout: 7_000 },

  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    [
      'monocart-reporter',
      {
        name: 'Stack86 e2e',
        outputFile: './coverage/index.html',
        logging: 'debug',
        coverage: {
          name: 'Stack86 coverage',
          outputDir: './coverage',
          entryFilter: (entry: { url: string }) =>
            (entry.url.includes(BASE_URL) || entry.url.includes('/assets/')) &&
            !entry.url.includes('editor.api'),
          sourceFilter: (sourcePath: string) => {
            if (!sourcePath.includes('src/app/')) return false;
            if (sourcePath.includes('.spec.')) return false;
            if (sourcePath.includes('/testing/')) return false;
            if (sourcePath.endsWith('.models.ts')) return false;
            if (sourcePath.endsWith('sample-programs.ts')) return false;
            if (sourcePath.includes('monaco-editor')) return false;
            return true;
          },
          reports: [['v8'], ['console-summary'], ['codecov']],
          // Pure-e2e thresholds. Codecov-style line coverage is 91.5% (lines hit / total
          // coverable lines). Monocart's stricter "executable line" metric runs lower
          // (~81%) because some instruction-handler branches require runtime states
          // (specific memory pointers, flag combinations) that browser e2e can't easily
          // synthesize without a unit-test layer.
          thresholds: {
            lines: 80,
            statements: 55,
            branches: 45,
            functions: 45,
          },
        },
      },
    ],
  ],

  use: {
    baseURL: BASE_URL,
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 10_000,
    navigationTimeout: 15_000,
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],

  webServer: {
    command: 'npm run build && npm run preview',
    url: BASE_URL,
    ignoreHTTPSErrors: true,
    reuseExistingServer: !process.env['CI'],
    timeout: 240_000,
    cwd: '..',
    env: {
      VITE_SOURCEMAPS: 'true',
    },
  },
});
